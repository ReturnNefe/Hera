# Hera

Hera is a simple and reliable flood attack software that you can use for stress testing.

*Warning: This software is only used for learning purposes, and all consequences are borne by you.*

## Supported Platforms

Hera targets `.net 6`, so it can run on Windows/Linux/macOS !

## Installing

You can download the program from the Release. Also, you can clone this repository.

```
git clone https://github.com/Return25/Hera.git
```

Then you can build it

```
cd Hera/Hera
dotnet restore
dotnet build
```

## Getting Started

Launch a simple HTTP flood attack

```
./hera flood --type Http --ip https://1.114.5.14
```

**Note, when you use Http mode, please add `http://` or `https://` to the --ip option**

After that, you can start an monitor to detect if the server is down.

```
./hera monitor --type Http --ip https://1.114.5.14
```

For more information about the command, enter commands
```
./hera -h
./hera flood -h
./hera monitor -h
```
