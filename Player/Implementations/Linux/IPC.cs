using System.IO.Pipes;
using System.Text;
#if DEBUG
using MWP.BackEnd.Helpers;
#endif

namespace MWP.Player.Implementations.Linux;

public class IPC : IDisposable
{
    private NamedPipeClientStream? pipeClient;
    private readonly string pipeName;
    private CancellationTokenSource? cts;
    private CancellationToken? ct => cts?.Token;
    private MessageQueue queue;
    private Task? _readTask;
    private bool isDisposed = false;
    
    public IPC(string pipeName, CancellationTokenSource cts, MessageQueue messageQueue)
    {
        this.pipeName = pipeName;
        this.cts = cts;
        queue = messageQueue;
    }

    public async Task<bool> ConnectAsync(int timeout = 1000)
    {
        if (ct == null) return false;
        try
        {
            pipeClient = new NamedPipeClientStream(".", pipeName,
                PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipeClient.ConnectAsync(timeout, (CancellationToken)ct);
            _readTask = ReadMessagesAsync();
            return true;
        }
        catch (TimeoutException e)
        {
#if DEBUG

            MyConsole.WriteLine(e);
            throw;
#endif
        }
        catch (OperationCanceledException e)
        {
#if DEBUG
            MyConsole.WriteLine(e);
#endif
        }
        catch (Exception e)
        {
#if DEBUG
            MyConsole.WriteLine(e);
            throw;
#endif
        }
        return false;
    }
    
    private async Task ReadMessagesAsync()
    {
        byte[] buffer = new byte[1024];
        MemoryStream memoryStream = new MemoryStream();

        while (ct is {IsCancellationRequested: false} && pipeClient is { IsConnected: true })
        {
            try
            {
                int bytesRead = await pipeClient.ReadAsync(buffer, (CancellationToken)ct);

                if (bytesRead == 0)
                {
                    continue;
                }

                memoryStream.Write(buffer, 0, bytesRead);

                // Check for complete message (ends with newline)
                if (memoryStream.ToArray().Length >= 2 && 
                    memoryStream.ToArray()[memoryStream.ToArray().Length - 2] == '\n')
                {
                    string message = Encoding.UTF8.GetString(memoryStream.ToArray());
                    queue.AddMessage(message);
                    
                    // Reset for next message
                    memoryStream.Position = 0;
                    memoryStream.SetLength(0);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex)
            {
#if DEBUG
                MyConsole.WriteLine($"Error reading from MPV: {ex.Message}");
#endif
                break;
            }
        }
    }

    public void WriteMessage(string message)
    {
        if (pipeClient is not { IsConnected: true }) return;
        pipeClient?.Write(Encoding.UTF8.GetBytes(message + "\n"));
        pipeClient?.Flush();
    }

    public void UpdateCts(CancellationTokenSource token)
    {
        cts = token;
    }

    public void Dispose()
    {
        if (isDisposed) return;
        cts?.Cancel();
        cts?.Dispose();
        pipeClient?.Close();
        pipeClient?.Dispose();
        pipeClient = null;
        cts = null;
        GC.SuppressFinalize(this);
        isDisposed = true;
    }
}