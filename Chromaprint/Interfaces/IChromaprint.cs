using MWP.Chromaprint.Datatypes;

namespace MWP.Chromaprint.Interfaces;

public interface IChromaprint
{
    public static string ClientKey = "b\'5LIvrD3L";
    public Task<ChromaprintResult?> Run(string filePath) => throw new NotImplementedException();
}