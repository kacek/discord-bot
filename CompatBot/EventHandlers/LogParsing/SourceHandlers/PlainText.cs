﻿using System;
using System.IO.Pipelines;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace CompatBot.EventHandlers.LogParsing.SourceHandlers
{
    public class PlainTextHandler: ISourceHandler
    {
        public Task<bool> CanHandleAsync(DiscordAttachment attachment)
        {
            return Task.FromResult(attachment.FileName.EndsWith(".log", StringComparison.InvariantCultureIgnoreCase));
        }

        public async Task FillPipeAsync(DiscordAttachment attachment, PipeWriter writer)
        {
            using (var client = HttpClientFactory.Create())
            using (var stream = await client.GetStreamAsync(attachment.Url).ConfigureAwait(false))
            {
                try
                {
                    int read;
                    FlushResult flushed;
                    do
                    {
                        var memory = writer.GetMemory(Config.MinimumBufferSize);
                        read = await stream.ReadAsync(memory, Config.Cts.Token);
                        writer.Advance(read);
                        flushed = await writer.FlushAsync(Config.Cts.Token).ConfigureAwait(false);
                    } while (read > 0 && !(flushed.IsCompleted || flushed.IsCanceled || Config.Cts.IsCancellationRequested));
                }
                catch (Exception e)
                {
                    Config.Log.Error(e, "Error filling the log pipe");
                    writer.Complete(e);
                    return;
                }
            }
            writer.Complete();
        }
    }
}
