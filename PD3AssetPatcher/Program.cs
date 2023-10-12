// See https://aka.ms/new-console-template for more information

using Newtonsoft.Json.Linq;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;
using System.IO;

static bool ByteArrayToFile(string fileName, byte[] byteArray)
{
    try
    {
        using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
        {
            fs.Write(byteArray, 0, byteArray.Length);
            return true;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Exception caught in process: {0}", ex);
        return false;
    }
}


Console.Write("Init\n");

if (args.Length == 0)
{
    Console.WriteLine("No arguments entered. Enter task and path to UAssets.\n");
    Console.WriteLine("-------------");
    Console.WriteLine("\nUsage:");
    Console.WriteLine("PD3AssetPatcher.exe AnimPatch \"path/to/assets/\"");
    Console.WriteLine("PD3AssetPatcher.exe AudioPatch \"path/to/assets/\"");
    Console.WriteLine("PD3AssetPatcher.exe AudioInfo \"path/to/assets/\"");
    Console.WriteLine("PD3AssetPatcher.exe ExtractWWiseData \"path/to/assets/\"");
    return;
}



if (Directory.Exists(args[1]))
{
    string[] allAssetPaths = Directory.GetFiles(args[1], "*.uasset", SearchOption.AllDirectories);
    foreach (string assetPath in allAssetPaths)
    {
        if (args[0] == "AnimPatch")
        {
            WriteAnimPatchData(assetPath);
        }
        else if (args[0] == "AudioPatch")
        {
            PatchAudioData(assetPath);
        }
        else if (args[0] == "AudioInfo")
        {
            GetAudioDataInfo(assetPath);
        }
        else if (args[0] == "ExtractWWiseData")
        {
            ExtractWWiseData(assetPath);
        }
        else
        {
            Console.WriteLine("Invalid Argument Entered");
        }
    }
}
else if (File.Exists(args[1]))
{
    if (args[0] == "AnimPatch")
    {
        WriteAnimPatchData(args[1]);
    }
    else if (args[0] == "AudioPatch")
    {
        PatchAudioData(args[1]);
    }
    else if (args[0] == "AudioInfo")
    {
        GetAudioDataInfo(args[1]);
    }
    else if (args[0] == "ExtractWWiseData")
    {
        ExtractWWiseData(args[1]);
    }
    else
    {
        Console.WriteLine("Invalid Argument Entered");
    }
}


static void WriteAnimPatchData(string assetPath)
{
    Console.Write("Found asset: ");
    Console.Write(assetPath);
    Console.Write("\n");
    UAsset myAsset = new UAsset(assetPath, EngineVersion.VER_UE4_27);

    foreach (Export export in myAsset.Exports)
    {
        if (export.GetExportClassType().Value.Value == "AnimSequence")
        {
            Console.Write("FoundAnimSeq\n");
            byte[] newExtras = new byte[export.Extras.Length + 4];
            Array.Copy(export.Extras, newExtras, export.Extras.Length);
            export.Extras = newExtras;
            myAsset.Write(assetPath);
            Console.Write("Asset Patched\n");
            break;
        }
    }
}

static float GetFloatInput(string prompt)
{
    while (true)
    {
        Console.WriteLine(prompt);
        string inputStr = Console.ReadLine();
        float parsed;
        if (float.TryParse(inputStr, out parsed))
        {
            return parsed;
        }
        Console.WriteLine("Failed to parse, try again.\n");
    }
}

static double GetDoubleInput(string prompt)
{
    while (true)
    {
        Console.WriteLine(prompt);
        string inputStr = Console.ReadLine();
        double parsed;
        if (double.TryParse(inputStr, out parsed))
        {
            return parsed;
        }
        Console.WriteLine("Failed to parse, try again.\n");
    }
}

static int Search(byte[] src, byte[] pattern, int start = 0)
{
    int maxFirstCharSlot = src.Length - pattern.Length + 1;
    for (int i = start; i < maxFirstCharSlot; i++)
    {
        if (src[i] != pattern[0]) // compare only first byte
            continue;
        // found a match on first byte, now try to match rest of the pattern
        for (int j = pattern.Length - 1; j >= 1; j--)
        {
            if (src[i + j] != pattern[j]) break;
            if (j == 1) return i;
        }
    }
    return -1;
}

static void PatchAudioData(string assetPath)
{
    Console.Write("Found asset: ");
    Console.Write(assetPath);
    Console.Write("\n");
    UAsset myAsset = new UAsset(assetPath, EngineVersion.VER_UE4_27);

    foreach (Export export in myAsset.Exports)
    {
        if (export.GetExportClassType().Value.Value == "AkAudioEventData")
        {
            Console.Write("Found Ak Event\n");

            ArrayPropertyData mediaArray = (ArrayPropertyData)((NormalExport)export)["MediaList"];
            foreach (ObjectPropertyData mediaObject in mediaArray.Value)
            {
                string mediaObjectName = mediaObject.ToImport(myAsset).ObjectName.Value.Value;
                Console.Write(mediaObjectName);
                Console.Write("\n");
                int.TryParse(mediaObjectName, out int value);
                Console.Write(value);


                int locationOfHeader = Search(export.Extras, BitConverter.GetBytes(value));

                int locationOf2ndMatchHeader = Search(export.Extras, BitConverter.GetBytes(value), locationOfHeader + 4) + 4;

                if (locationOf2ndMatchHeader != -1)
                {
                    double fPlayAt = GetDoubleInput("Enter Play At in milliseconds:");

                    double fBeginTrimOffset = GetDoubleInput("Enter Begin Trim-Offset in seconds:");

                    double fEndTrimOffset = GetDoubleInput("Enter End Trim-Offset in seconds:");

                    double fSrcDuration = GetDoubleInput("Enter Source Duration in milliseconds:");

                    /*float fadeOutEndFloat = GetFloatInput("Enter Fade-Out End in seconds:");

                    double maxDurationDouble = GetDoubleInput("Enter Maximum Music Duration in milliseconds:");

                    double loopStartDouble = GetDoubleInput("Enter Loop-Start in milliseconds:");

                    double loopEndDouble = GetDoubleInput("Enter Loop-End in milliseconds:");*/

                    Buffer.BlockCopy(BitConverter.GetBytes(fPlayAt), 0, export.Extras, locationOf2ndMatchHeader + 0x54, 8);
                    Buffer.BlockCopy(BitConverter.GetBytes(fBeginTrimOffset), 0, export.Extras, locationOf2ndMatchHeader + 0x70, 8);
                    Buffer.BlockCopy(BitConverter.GetBytes(fEndTrimOffset), 0, export.Extras, locationOf2ndMatchHeader + 0x7C, 8);
                    Buffer.BlockCopy(BitConverter.GetBytes(fSrcDuration), 0, export.Extras, locationOf2ndMatchHeader + 0xA0, 8);
                    /*Buffer.BlockCopy(BitConverter.GetBytes(fadeOutEndFloat), 0, export.Extras, locationOf2ndMatchHeader + 0xAC, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(maxDurationDouble), 0, export.Extras, locationOf2ndMatchHeader + 0x12F, 8);
                    Buffer.BlockCopy(BitConverter.GetBytes(loopStartDouble), 0, export.Extras, locationOf2ndMatchHeader + 0x13F, 8);
                    Buffer.BlockCopy(BitConverter.GetBytes(loopEndDouble), 0, export.Extras, locationOf2ndMatchHeader + 0x14C, 8);*/

                    myAsset.Write(assetPath);
                    Console.Write("Asset Patched\n");
                }
                else
                {
                    Console.Write("Match not found\n");
                }
            }
        }

        if (export.GetExportClassType().Value.Value == "AkMediaAssetData")
        {
            Console.Write("Found Ak Media\n");
            string[] assetPathSplit = assetPath.Split(".");
            string uBulkAssetPath = string.Concat(assetPathSplit[0], ".ubulk");
            if (File.Exists(uBulkAssetPath))
            {
                Console.Write("Found uBulk: {0}\n", uBulkAssetPath);
                FileInfo fi = new FileInfo(uBulkAssetPath);
                int uBulkSize = (int)fi.Length;
                Console.Write("uBulk Len: {0}\n", uBulkSize.ToString());
                if (BitConverter.ToInt32(export.Extras, 0x10) == uBulkSize)
                {
                    Console.WriteLine("uBulk Size matches, skipping...\n");
                }
                else
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(uBulkSize), 0, export.Extras, 0x10, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(uBulkSize), 0, export.Extras, 0x14, 4);
                    myAsset.Write(assetPath);
                    Console.Write("Patched uExp\n");
                }
            }
        }
    }
}

static void GetAudioDataInfo(string assetPath)
{

}

static void ExtractWWiseData(string assetPath)
{
    Console.Write("Found asset: ");
    Console.Write(assetPath);
    Console.Write("\n");
    UAsset myAsset = new UAsset(assetPath, EngineVersion.VER_UE4_27);
    var bnkPattern = new byte[] { 0x42, 0x4B, 0x48, 0x44 };
    var audioPattern = new byte[] { 0x52, 0x49, 0x46, 0x46 };
    foreach (Export export in myAsset.Exports)
    {
        if (export.GetExportClassType().Value.Value == "AkAudioEventData" || export.GetExportClassType().Value.Value == "AkInitBankAssetData")
        {
            Console.Write("Found Ak Event\n");
            int locationOfHeader = Search(export.Extras, bnkPattern);
            byte[] bnkData = new byte[export.Extras.Length - locationOfHeader];
            Array.ConstrainedCopy(export.Extras, locationOfHeader, bnkData, 0x0, export.Extras.Length - locationOfHeader);
            string newAssPathConcat = String.Concat((assetPath.Split("."))[0], ".bnk");
            ByteArrayToFile(newAssPathConcat, bnkData);
        }
        else if (export.GetExportClassType().Value.Value == "AkMediaAssetData")
        {
            Console.Write("Found Ak Media\n");
            int locationOfHeader = Search(export.Extras, audioPattern);
            string ubulkPath = String.Concat((assetPath.Split("."))[0], ".ubulk");
            File.Copy(ubulkPath, String.Concat((assetPath.Split("."))[0], ".wav"));
        }
    }
}
