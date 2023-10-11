// See https://aka.ms/new-console-template for more information

using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;




Console.Write("Init\n");

if (args.Length == 0)
{
    Console.WriteLine("No arguments entered. Enter task and path to UAssets.\n");
    Console.WriteLine("-------------");
    Console.WriteLine("\nUsage:");
    Console.WriteLine("PD3AssetPatcher.exe AnimPatch \"path/to/assets/\"");
    Console.WriteLine("PD3AssetPatcher.exe AudioPatch \"path/to/assets/\"");
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

                int locationOf2ndMatchHeader = Search(export.Extras, BitConverter.GetBytes(value), locationOfHeader + 4);

                if (locationOf2ndMatchHeader != -1)
                {
                    double durationDouble = GetDoubleInput("Enter Audio Duration in milliseconds:");

                    float fadeInStartFloat = GetFloatInput("Enter Fade-In Start in seconds:");

                    float fadeInEndFloat = GetFloatInput("Enter Fade-In End in seconds:");

                    float fadeOutStartFloat = GetFloatInput("Enter Fade-Out Start in seconds:");

                    float fadeOutEndFloat = GetFloatInput("Enter Fade-Out End in seconds:");

                    double maxDurationDouble = GetDoubleInput("Enter Maximum Music Duration in milliseconds:");

                    double loopStartDouble = GetDoubleInput("Enter Loop-Start in milliseconds:");

                    double loopEndDouble = GetDoubleInput("Enter Loop-End in milliseconds:");

                    Buffer.BlockCopy(BitConverter.GetBytes(durationDouble), 0, export.Extras, locationOf2ndMatchHeader + 0x54, 8);
                    Buffer.BlockCopy(BitConverter.GetBytes(fadeInStartFloat), 0, export.Extras, locationOf2ndMatchHeader + 0xA4 - 0x34, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(fadeInEndFloat), 0, export.Extras, locationOf2ndMatchHeader + 0xB0 - 0x34, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(fadeOutStartFloat), 0, export.Extras, locationOf2ndMatchHeader + 0xD4 - 0x34, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(fadeOutEndFloat), 0, export.Extras, locationOf2ndMatchHeader + 0xE0 - 0x34, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(maxDurationDouble), 0, export.Extras, locationOf2ndMatchHeader + 0x163 - 0x34, 8);
                    Buffer.BlockCopy(BitConverter.GetBytes(loopStartDouble), 0, export.Extras, locationOf2ndMatchHeader + 0x173 - 0x34, 8);
                    Buffer.BlockCopy(BitConverter.GetBytes(loopEndDouble), 0, export.Extras, locationOf2ndMatchHeader + 0x180 - 0x34, 8);

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

