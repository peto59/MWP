using System.Diagnostics;
using Newtonsoft.Json;

namespace MWP.BackEnd.Chromaprint;

public class ChromaprintLinux : IChromaprint
{
    public async Task<ChromaprintResult?> Run(string filePath)
    {
        using Process process = new Process();
        process.StartInfo.FileName = "fpcalc";
        process.StartInfo.Arguments = $"-json \"{filePath}\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
            
        process.Start();
        string fpResult = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
            
        return JsonConvert.DeserializeObject<ChromaprintResult>(fpResult);
    }
}