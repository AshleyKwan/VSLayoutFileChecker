

using Microsoft.VisualStudio.Setup;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using VSLayoutFile;


public class VSLayoutFileChecker
{

    enum AutoDownloadOption
    {
        Ask,
        AutoDownload,
        NotDownload
    }

    static Dictionary<string, string> envirVarrible = new Dictionary<string, string>();
    public static void Main(string[] args)
    {
        string layoutPath = null!;
        string packagePath = null!;
        string catalogPath = null!;
        AutoDownloadOption autoDownload = AutoDownloadOption.Ask;
        string packageId = null!;
        bool noDynamicEndpointAllow = false;
        bool isAutoFix = false;
        string productID = null!;
        // string channelManifestFilePath;// = "";

        string[] locale = { null!, "neutral", "en-us", "zh-tw", "ja-jp" };// , CultureInfo.CurrentCulture.Name.ToLower() };
        string[] packageTypeExcept = { "component", "group", "product", "workload" };
        string[] productArch = { null!, "neutral", "x86", "x64", "arm64" };
        string[] macingeArch = { null!, "neutral", "x86", "x64", "arm64" };

        Dictionary<string, string> errPackagePayload = new Dictionary<string, string>();


        //split the args for the command line
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--layoutpath":
                    layoutPath = args[i + 1];
                    i++;
                    break;

                case "--catalog":
                    catalogPath = args[i + 1];
                    i++;
                    break;
                case "--download": // the option for auto download missing file.
                    if (autoDownload == AutoDownloadOption.NotDownload)
                    {
                        Console.WriteLine("Conflict download options.");
                        Console.ReadKey();
                    }
                    autoDownload = AutoDownloadOption.AutoDownload;
                    break;
                case "--notdownload": // the option for do not auto download missing file.
                    if (autoDownload == AutoDownloadOption.AutoDownload)
                    {
                        Console.WriteLine("Conflict download options.");
                        Console.ReadKey();
                    }
                    autoDownload = AutoDownloadOption.NotDownload;
                    break;
                case "--packageid":
                    packageId = args[i + 1];
                    i++;
                    break;
                case "--productid":
                    productID = args[i + 1];
                    i++;
                    break;

                case "--nodynamicendpoint":
                    noDynamicEndpointAllow = true;
                    break;
                case "--fix": // the option for auto redownload invalid file.
                    isAutoFix = true;
                    break;
                case "--lang":
                    List<string> lang = new List<string>();

                    lang.AddRange(new string[] { null!, "neutral" });

                    for (int j = i + 1; j < args.Length; j++)
                    {
                        if (args[j].StartsWith("--"))
                        {
                            break;
                        }
                        else
                        {
                            lang.Add(args[j].ToLower());
                        }
                    }

                    locale = lang.ToArray();
                    break;
                case "--productarch":
                    List<string> arch = new List<string>();

                    arch.Add(null!);
                    for (int j = i + 1; j < args.Length; j++)
                    {
                        if (args[j].StartsWith("--"))
                        {
                            break;
                        }
                        else
                        {
                            arch.Add(args[j].ToLower());
                        }
                    }

                    foreach (string a in arch)
                    {
                        if (a == null)
                            continue;
                        if (a.ToLower() != "neutral" && a.ToLower() != "x86" && a.ToLower() != "x64" && a.ToLower() != "arm64")
                        {
                            Console.WriteLine($"The product arch {a} is not supported.");
                            return;
                        }
                    }

                    productArch = arch.ToArray();
                    break;
            }
        }

        //Initialize the envirVarrible
        envirVarrible.Add("[catalogPath]", catalogPath);
        envirVarrible.Add("[channelId]", "");
        envirVarrible.Add("[channelUri]", "");
        envirVarrible.Add("[CommonApplicationData]", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        envirVarrible.Add("[installDir]", "");
        envirVarrible.Add("[InstanceId]", "");
        envirVarrible.Add("[installChannelUri]", "");
        envirVarrible.Add("[LogFile]", "");
        envirVarrible.Add("[PackageDir]", $"{layoutPath}\\{packagePath}");
        envirVarrible.Add("[PackageLayoutDir]", $"{layoutPath}\\{packagePath}"); /// the path for layout, including package name and version....etc
        envirVarrible.Add("[ProgramFiles]", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        envirVarrible.Add("[ProgramFilesx64]", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        envirVarrible.Add("[Payload]", "");
        envirVarrible.Add("[SystemFolder]", Environment.GetFolderPath(Environment.SpecialFolder.System));
        envirVarrible.Add("[startmenu]", Environment.GetFolderPath(Environment.SpecialFolder.StartMenu));
        //   envirVarrible.Add("[SharedInstallDrive]",Environment.GetFolderPath(Environment..s))

        Console.WriteLine($"The layout path is {layoutPath}.");

        if (Path.Exists(layoutPath))
        {
            if (catalogPath == null)
            {
                catalogPath = $"{layoutPath}\\catalog.json";
            }


            if (File.Exists(catalogPath))
            {

                string jsonString = File.ReadAllText(catalogPath);



                VSLayoutFile.ChannelManifest channelManifest = JsonSerializer.Deserialize<VSLayoutFile.ChannelManifest>(jsonString)!;
                //    channelManifest.ConstructDependencies();

                Console.WriteLine();
                Console.WriteLine($"The found manifest catalog :");
                Console.WriteLine($"Manifest version :{channelManifest.manifestVersion}");
                Console.WriteLine($"Product :{channelManifest.info.productName} {channelManifest.info.productLineVersion}");
                Console.WriteLine($"Version :{channelManifest.info.productDisplayVersion}");
                Console.WriteLine($"Required engine version :{channelManifest.engineVersion}");

                Console.WriteLine();
                if (channelManifest.manifestVersion > new Version(1, 1))
                {
                    Console.WriteLine("The manifest version is higher then supported, please check the version.");
                    Console.ReadKey();
                    return;
                }

                if (channelManifest.engineVersion > new Version(3, 13, 2609, 59209))
                {
                    Console.WriteLine("The engine version is higher then supported, please check the version.");
                    //  Console.ReadKey();
                    //  return;
                }

                if (productID == null)
                {
                    // get the product items in the catalog
                    List<ChannelPackage> catalogProduct = channelManifest.FindPackageByType("Product");
                    Console.WriteLine("The Catalog contain following product:");
                    for (int i = 0; i < catalogProduct.Count; i++)
                    {
                        Console.WriteLine($"{i + 1}: {catalogProduct[i].id};\t\t\t\t\t\tProduct Arch:{catalogProduct[i].productArch}");
                    }

                    //Console.WriteLine();
                    //Console.WriteLine("Please input the product index to check:(-1=All)");
                    //while (true)
                    //{
                    //    string? userInput = Console.ReadLine();
                    //    if (int.TryParse(userInput, out int productIndex))
                    //    {
                    //        if (productIndex == -1)
                    //        {
                    //            Console.WriteLine("All packages selected.");
                    //            continue;
                    //        }
                    //        if (productIndex > 0 && productIndex <= catalogProduct.Count)
                    //        {
                    //            productID = catalogProduct[productIndex - 1].id;
                    //            Console.WriteLine($"Selected product: {catalogProduct[productIndex - 1].id}");
                    //            break;
                    //        }
                    //    }
                    //    Console.WriteLine("Invalid input. Please enter a valid product index:");
                    //}
                }


                List<ChannelPackage> packages = channelManifest.packages;// new List<ChannelPackage>();


                if (packageId != null && packageId != "")
                    packages = channelManifest.FindPackageById(packageId);





                foreach (ChannelPackage package in packages)
                {
                    if (!packageTypeExcept.Contains(package.type.ToLower()))// package.type.ToLower() != "component" || package.type.ToLower() != "group")
                    {


                        if (!locale.Contains((package.language != null) ? package.language.ToLower() : null))
                        {
                            continue;
                        }

                        if (!productArch.Contains((package.productArch != null) ? package.productArch.ToLower() : null))
                        {
                            continue;
                        }

                        packagePath = ($"{package.id},version={package.version}{(package.chip != null ? $",chip={package.chip}" : "")}{(package.language != null ? $",language={package.language}" : "")}{(package.productArch != null ? $",productArch={package.productArch}" : "")}{(package.machineArch != null ? $",machineArch={package.machineArch}" : "")}");
                          envirVarrible["[PackageDir]"] =$"{layoutPath}\\{packagePath}";
                        envirVarrible["[PackageLayoutDir]"] = $"{layoutPath}\\{packagePath}";

                        Console.WriteLine();
                        Console.WriteLine($"{packagePath}...");

                        if (package.payloads == null)
                            continue;

                        foreach (ChannelPayload payload in package.payloads)
                        {
                            Console.Write($"    {payload.fileName}...");

                            if (payload.isDynamicEndpoint && !noDynamicEndpointAllow)
                            {

                                errPackagePayload[$"{packagePath}\\{payload.fileName}"] = " is dynamic endpoint.";

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"is dynamic endpoint. Skip checking.");
                                Console.ForegroundColor = ConsoleColor.White;
                                continue;
                            }


                            envirVarrible["[Payload]"] = $"{layoutPath}\\{packagePath}\\{payload.fileName}";

                            //  string payloadFileFullName = $"{layoutPath}\\{packagePath}\\{payload.fileName}";

                            SHA256 sha256 = SHA256.Create();
                            ConsoleKeyInfo userKeyInfo = new ConsoleKeyInfo();
                            sha256.Clear();

                            string payloadFileExtention = "";

                            for (int i = payload.fileName.Length; --i >= 0;)
                            {
                                char ch = payload.fileName[i];
                                if (ch == '.')
                                {
                                    payloadFileExtention = payload.fileName.Substring(i, payload.fileName.Length - i);
                                    break;
                                }
                            }


                            if (payloadFileExtention.ToLower() == ".vsix")
                            {
                                if (!File.Exists(envirVarrible["[Payload]"]))
                                {
                                    Console.WriteLine($"{payload.fileName} is not existed. Looking the payload.vsix");
                                    envirVarrible["[Payload]"] = $"{layoutPath}\\{packagePath}\\payload.vsix";
                                }
                            }


                            if (File.Exists(envirVarrible["[Payload]"]))
                            {
                                if (payload.sha256 == null)
                                {
                                    errPackagePayload[$"{packagePath}\\{payload.fileName}"] = " not include sha2 value.";
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"{payload.fileName} not include sha2 value, no check sha256.", Console.ForegroundColor);
                                    Console.ForegroundColor = ConsoleColor.White;
                                    continue;

                                }

                                do
                                {
                                    if (!VerifyFile(envirVarrible["[Payload]"], payload.sha256))
                                    {
                                        errPackagePayload[$"{packagePath}\\{payload.fileName}"] = " is not vaild.";

                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.WriteLine($"is not vaild.", Console.ForegroundColor);
                                        Console.ForegroundColor = ConsoleColor.White;

                                        if (!isAutoFix)
                                        {
                                            Console.WriteLine("Do you want to re-verify it? (Y:Yes, N:No, D:Re-download)");
                                            userKeyInfo = Console.ReadKey();
                                            Console.Write("...");
                                        }
                                    }
                                    else
                                    {
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("OK", Console.ForegroundColor);
                                        Console.ForegroundColor = ConsoleColor.White;


                                        break;
                                    }

                                    if (userKeyInfo.Key == ConsoleKey.Y)
                                        continue;
                                    else if (userKeyInfo.Key == ConsoleKey.N)
                                        break;
                                    else if (userKeyInfo.Key == ConsoleKey.D || isAutoFix)
                                    {
                                        try
                                        {
                                            DownloadFileHandler(payload.url, envirVarrible["[Payload]"], layoutPath, packagePath, payload.sha256!);
                                            break;
                                        }
                                        catch (WebException)
                                        {
                                            //Console.WriteLine($"Error with exception:{payload.url}");
                                            break;
                                        }

                                    }
                                } while (true);
                            }
                            else
                            {
                                errPackagePayload[$"{packagePath}\\{payload.fileName}"] = " file not exist.";

                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"file not exist.", Console.ForegroundColor);
                                Console.ForegroundColor = ConsoleColor.White;

                                if (autoDownload == AutoDownloadOption.NotDownload)
                                {
                                    Console.WriteLine("Skip download.");
                                    continue;
                                }

                                while (true)
                                {

                                    if (autoDownload == AutoDownloadOption.Ask && !isAutoFix)
                                    {
                                        Console.WriteLine("Do you want to download it? (Y/N)");
                                        userKeyInfo = Console.ReadKey();
                                    }

                                    if (autoDownload == AutoDownloadOption.AutoDownload || isAutoFix || userKeyInfo.Key == ConsoleKey.Y)
                                    {
                                        //  payloadFileFullName = $"{layoutPath}\\{packagePath}\\{payload.fileName}";
                                        envirVarrible["[Payload]"] = $"{layoutPath}\\{packagePath}\\{payload.fileName}"; //reset the envirVarrible "Payload" to original file name in catalog.
                                        try
                                        {
                                            DownloadFileHandler(payload.url, envirVarrible["[Payload]"], layoutPath, packagePath, payload.sha256!);                                           
                                        }
                                        catch (WebException)
                                        {
                                            //Console.WriteLine($"Error with exception:{payload.url}");
                                            break ;
                                        }
                                    }
                                    else if (userKeyInfo.Key == ConsoleKey.N)
                                    {
                                        Console.WriteLine("Skip download.");
                                        break;
                                        //Console.WriteLine("Do you want to re-verify it? (Y:Yes, N:No)");
                                        //userKeyInfo = Console.ReadKey();

                                        //while (userKeyInfo.Key == ConsoleKey.Y)
                                        //{
                                        //    if (!VerifyFile(payloadFileFullName, payload.sha256!))
                                        //    {
                                        //        Console.ForegroundColor = ConsoleColor.Red;
                                        //        Console.WriteLine($"is not vaild.", Console.ForegroundColor);
                                        //        Console.ForegroundColor = ConsoleColor.White;

                                        //        Console.WriteLine("Do you want to re-verify it? (Y:Yes, N:No)");
                                        //        userKeyInfo = Console.ReadKey();
                                        //        Console.Write("...");
                                        //    }
                                        //    else
                                        //    {
                                        //        Console.ForegroundColor = ConsoleColor.Green;
                                        //        Console.WriteLine("OK", Console.ForegroundColor);
                                        //        Console.ForegroundColor = ConsoleColor.White;

                                        //        break;
                                        //    }
                                        //}// while (userKeyInfo.Key == ConsoleKey.Y);

                                    }
                                    else
                                    {
                                        continue;
                                    }

                                }

                            }
                        }

                        //if (package.layoutParams != null)
                        //{
                        //    Console.WriteLine("The package has layout params to be run, Execute the parms? (Y:Yes, N:No)");
                        //    ConsoleKeyInfo userKey =Console.ReadKey();

                        //    if (userKey.Key == ConsoleKey.Y)
                        //    {
                        //        Console.WriteLine("Execution layout params...");
                        //        int r = executeCommand(package.layoutParams!.fileName, package.layoutParams!.parameters);
                        //        if (r != 0)
                        //        {
                        //            Console.ForegroundColor = ConsoleColor.Red;
                        //            Console.WriteLine("Layout params execution with error. Exit code:" + r);
                        //            Console.ForegroundColor = ConsoleColor.White;
                        //            Console.ReadLine();
                        //        }
                        //    }

                        //    //switch (r)
                        //    //{                                
                        //    //    case 1603:
                        //    //        Console.ForegroundColor = ConsoleColor.Red;
                        //    //        Console.WriteLine("Layout params execution with error.");
                        //    //        Console.ForegroundColor = ConsoleColor.White;
                        //    //        break;
                        //    //}
                        //}
                        //need to rewrite the layout params
                        //console.WriteLine(package.layoutParams);
                    }
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("All files checking is done.");
                Console.ForegroundColor = ConsoleColor.White;
                //   Console.ReadKey();
                Console.WriteLine(new string('=', 80));
                foreach (string a in errPackagePayload.Keys)
                {
                    Console.WriteLine($"{a} {errPackagePayload[a]}");
                }
            }
            else
            {
                Console.WriteLine("Cannot find the catalog file.");
            }
        }
        else
        {
            Console.WriteLine("Cannont find the layout path.");
            Console.ReadKey();
        }

        //Console.WriteLine("Hello, World!");

        // string a = JsonConvert //JsonSerializer.Deserialize("");

    }

    public static void DownloadFileHandler(string url, string fullFileName, string layoutPath, string packagePath, string hash)
    {
        Console.WriteLine("Downloading payload by WebClient...");

        try
        {
            DownloadFile(url, fullFileName, layoutPath, packagePath, hash);
        }
        catch (WebException)
        {
            throw;//  continue;
        }


        Console.WriteLine($"Download completed.");
        Console.Write($"Verifying file...");
        do
        {
            ConsoleKeyInfo userKeyInfo;

            if (!VerifyFile(fullFileName, hash))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"is not vaild.", Console.ForegroundColor);
                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine("Do you want to re-verify it? (Y:Yes, N:No)");
                userKeyInfo = Console.ReadKey();

            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("OK", Console.ForegroundColor);
                Console.ForegroundColor = ConsoleColor.White;

                break;
            }

            if (userKeyInfo.Key == ConsoleKey.Y)
                continue;
            else if (userKeyInfo.Key == ConsoleKey.N)
                break;
        } while (true);


    }


    public static Boolean VerifyFile(string filePath, string hash)
    {
        try
        {
            SHA256 sha256 = SHA256.Create();
            FileStream file = new FileInfo(filePath).Open(System.IO.FileMode.Open);
            string fileHash = Convert.ToHexString(sha256.ComputeHash(file));
            file.Close();

            if (fileHash.ToLower() != hash.ToLower())
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        catch (System.IO.IOException err)
        {

            Console.WriteLine($"Error with exception:{err.Message}");
            //  Console.ReadKey();
            return false;
        }
    }

    public List<ChannelPayload> GetAllPayLoad(ChannelPackage package)
    {
        List<ChannelPackage> packages = new List<ChannelPackage>();

        List<ChannelPayload> payload = new List<ChannelPayload>();

        foreach (KeyValuePair<string, object> d in package.dependencies)
        {
            //   ChannelPackage p = package.FindPackageById(d.Key)[0];
        }
        return payload;
    }

    public static bool DownloadFile(string url, string fullFileName, string layoutPath, string packagePath, string hash)
    {
        using (WebClient webClient = new())
        {

            try
            {
                string packageSubfolder = fullFileName.Substring(0, fullFileName.LastIndexOf("\\"));
                System.IO.Directory.CreateDirectory(packageSubfolder);// $"{layoutPath}\\{packagePath}");
                webClient.DownloadFile(url, $"{fullFileName}");


                /*
                if (!VerifyFile($"{layoutPath}\\{packagePath}\\{fileName}", hash))
                {

                    return false;
                }*/

                return true;
            }
            catch (Exception err)
            {
                Console.WriteLine($"Error with exception:{err.Message}");
                Console.ReadKey();
                throw;
            }
        }
    }

    //static int executeCommand(string fileName, string parameters)
    //{

    //    foreach (string v in envirVarrible.Keys)
    //    {
    //        fileName = fileName.Replace(v, envirVarrible[v]);
    //        parameters = parameters.Replace(v, envirVarrible[v]);
    //    }


    //    ProcessStartInfo processInfo = new ProcessStartInfo();
    //    processInfo.FileName = fileName;
    //    processInfo.Arguments = parameters;


    //    Process p = new();
        
    //    p.StartInfo = processInfo;
    //    p.Start();
    //    p.WaitForExit();
        
    //    return p.ExitCode;
    //}


    class RefString(string value)
    {
        public string Value { get; set; } = value;
    }

}

