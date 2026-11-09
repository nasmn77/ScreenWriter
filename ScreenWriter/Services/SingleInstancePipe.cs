using System.IO;
using System.IO.Pipes;
using System.Text;

namespace ScreenWriter.Services;

/// <summary>Lets a second launch hand a file to the instance that is already running.</summary>
public sealed class SingleInstancePipe : IDisposable
{
    private const string PipeName = "ScreenWriter_Open_A3F2B1C4";
    private const int MaxPathChars = 32_768;

    private readonly CancellationTokenSource _cts = new();

    /// <summary>Sends a path to the running instance. Returns false if none answered.</summary>
    public static bool Send(string path)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            client.Connect(3000);
            using var writer = new StreamWriter(client, new UTF8Encoding(false));
            writer.Write(path);
            writer.Flush();
            return true;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Starts listening; onPath runs on a background thread.</summary>
    public void Listen(Action<string> onPath) => _ = Task.Run(() => LoopAsync(onPath, _cts.Token));

    private static async Task LoopAsync(Action<string> onPath, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(token);

                using var reader = new StreamReader(server, Encoding.UTF8);
                var buffer = new char[MaxPathChars + 1];
                int length = await reader.ReadBlockAsync(buffer, token);
                if (length is > 0 and <= MaxPathChars)
                    onPath(new string(buffer, 0, length));
            }
            catch (OperationCanceledException) { return; }
            catch (IOException) { await Task.Delay(200, CancellationToken.None); }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
