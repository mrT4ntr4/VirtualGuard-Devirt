using AsmResolver.DotNet;
using AsmResolver.DotNet.Builder;
using System;
using System.IO;
using static VirtualGuardDevirt.Logger;

namespace VirtualGuardDevirt
{
    internal class Context
    {

        public static ModuleDefinition module = null;
        public static string FileName = null;
        public static void LoadModule(string filename)
        {
            try
            {
                FileName = filename;
                module = ModuleDefinition.FromFile(filename);
                Log("Module Loaded : " + module.Name, TypeMessage.Info);
                Console.WriteLine();
            }
            catch
            {
                Log("Error while loading Module", TypeMessage.Error);
            }
        }

        public static void SaveModule()
        {
            try
            {
                string filename = string.Concat(new string[] { Path.GetFileNameWithoutExtension(FileName), "_Devirt", Path.GetExtension(FileName) });
                var imageBuilder = new ManagedPEImageBuilder();

                var factory = new DotNetDirectoryFactory();
                factory.MetadataBuilderFlags = MetadataBuilderFlags.PreserveAll;
                imageBuilder.DotNetDirectoryFactory = factory;

                module.Write(filename, imageBuilder);
                Log("File Devirt and Saved : " + filename, TypeMessage.Done);
            }
            catch (Exception ex)
            {
                Log("Failed to save current module\n" + ex.ToString(), TypeMessage.Error);
            }
        }

        public static void Welcome()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(@"                    _.---._     .---.                 / _ \              ");
            Console.WriteLine(@"           __...---' .---. `---'-.   `.             \_\(_)/_/            ");
            Console.WriteLine(@"       .-''__.--' _.'( | )`.  `.  `._ :              _//o\\_ Max         ");
            Console.WriteLine(@"     .'__-'_ .--'' ._`---'_.-.  `.   `-`.             /   \              ");
            Console.WriteLine(@"            ~ -._ -._``---. -.    `-._   `.                              ");
            Console.WriteLine(@"                 ~ -.._ _ _ _ ..-_ `.  `-._``--.._                       ");
            Console.WriteLine(@"                              -~ -._  `-.  -. `-._``--.._.--''.          ");
            Console.WriteLine(@"         VirtualGuard              ~ ~-.__     -._  `-.__   `. `.        ");
            Console.WriteLine(@"    Crocodile/Spider VM              jgs ~~ ~---...__ _    ._ .` `.      ");
            Console.WriteLine(@"                 Devirt by MrT4ntr4                  ~  ~--.....--~      ");
            Console.WriteLine();
            Console.WriteLine(@"-------------------------------------------------------------------------");
            Console.WriteLine();

            Console.ResetColor();
        }

    }
}
