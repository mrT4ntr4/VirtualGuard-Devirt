using AsmResolver.DotNet;
using System;
using static VirtualGuardDevirt.Context;
using static VirtualGuardDevirt.Logger;

namespace VirtualGuardDevirt
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                Welcome();
                LoadModule(args[0]);
                bool isCrocodileVM = false;
                bool isSpiderVM = false;

                foreach (ManifestResource res in module.Resources)
                {
                    if (res.Name == "crocodile")
                        isCrocodileVM = true;
                    else if (res.Name == "spider")
                        isSpiderVM = true;
                }

                if (isCrocodileVM)
                {
                    Log("VM Type : Crocodile VM", TypeMessage.Info);
                    Protections.CrocodileVM.VM.Execute();
                }
                else if (isSpiderVM)
                {
                    Log("VM Type : Spider VM", TypeMessage.Info);
                    Protections.SpiderVM.VM.Execute();
                }
                else
                {
                    throw new Exception("VM Type : UNKNOWN (Possibility of No/New Virtualization)");
                }

                SaveModule();
            }
            else
            {
                Log($"Please drag and drop your file\n\n", TypeMessage.Error);
            }
        }
    }
}
