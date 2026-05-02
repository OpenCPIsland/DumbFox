using DumbFox.Handlers;
using Common.Utils;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using Sfs2X.Util;
using System.Net.Sockets;
using System.Text;

namespace DumbFox;

public static class ClientManager
{
    public static async Task HandleClient(TcpClient client, CancellationToken ct = default)
    {
        Console.WriteLine("Client connected");
        
        Session session = new Session(client);

        try
        {
            client.NoDelay = true;

            NetworkStream stream = session.Stream;
            
            while (!ct.IsCancellationRequested)
            {
                (byte flags, byte[] payload)? pkt = await SfsRead.ReadPacketAsync(stream, ct);
                if (pkt == null)
                {
                    Console.WriteLine("Client disconnected (EOF)");
                    break;
                }

                (byte flags, byte[] payload) = pkt.Value;
                
                SFSObject outer;
                try
                {
                    outer = SFSObject.NewFromBinaryData(new ByteArray(payload));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Decode error: {ex.Message}");
                    break;
                }
                
                if (!outer.ContainsKey("c") || !outer.ContainsKey("a") || !outer.ContainsKey("p"))
                {
                    Console.WriteLine("Invalid packet: missing c/a/p");
                    break;
                }

                byte controller = outer.GetByte("c");
                short action = outer.GetShort("a");
                if (outer.GetSFSObject("p") is not SFSObject p)
                {
                    Console.WriteLine("Invalid p object type");
                    return;
                }

                Console.WriteLine($"controller={controller} action={action} keys=[{string.Join(",", p.GetKeys())}]");
                
                await RequestHandler.HandleRequest(action, controller, p, session);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Client manager canceled");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IO error: {ex.Message}");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled client error: {ex}");
        }
        finally
        {
            session.Cleanup();
            try { client.Close(); } catch { /* ignore */ }
            Console.WriteLine("Client closed");
        }
    }
}
