using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public class CsprojPostprocessor: AssetPostprocessor {


    private static Dictionary<string, string> ModBundleMap = new Dictionary<string, string>(){
        {"MODNAME","ASSETBUNDL"} ///EDIT WITH NAME OF MOD ASSEMBLY AND NAME OF ASSET BUNDLE (CASE MATTERS)
    };



    private static List<string> folders = new List<string>() {
        "Libraries"
    };

    private static Dictionary<string, string> folderMap = new Dictionary<string, string>() {
        {"CardChoiceSpawnUniqueCardPatch","Libraries"},
        {"CardThemeLib","Libraries"},
        {"ClassesManagerReborn","Libraries"},
        {"ItemShops","Libraries"},
        {"ModdingUtils","Libraries"},
        {"ModsPlus","Libraries"},
        {"PickNCards","Libraries"},
        {"RarityLib","Libraries"},
        {"RoundsWithFriends","Libraries"},
        {"UnboundLib","Libraries"},
        {"WillsWackyManagers","Libraries"},
        {"ILGenerator","Libraries"},
    };


    public static string OnGeneratedCSProject(string path, string content) {
        foreach(var mod in ModBundleMap.Keys){
            if(path.EndsWith($"{mod}.csproj")){
                string newContent = "";
                bool Added = false;
                var lines = content.Split('\n');
                foreach(var line in lines){
                    if(!Added && line.Contains("<Compile Include=")){
                        newContent += $"     <EmbeddedResource Include=\"Assets\\AssetBundles\\{ModBundleMap[mod]}\" />\n";
                        Added = true;
                    }
                    newContent += line + "\n";
                }
                return newContent;
            }
        }
        return content;
    }

    public static string OnGeneratedSlnSolution(string path, string content) {
        string newContent = "";
        Dictionary<string, string> folderGuids = new Dictionary<string, string>();
        foreach(string folder in folders) {
            folderGuids[folder] = Guid.NewGuid().ToString().ToUpper();
        }
        Dictionary<string, string> modGuids = new Dictionary<string, string>();
        var lines = content.Split('\n').Select(line => line.Trim('\r'));
        bool setup = false;
        foreach(string line in lines) {
            if(!setup) {
                if(line == "Global") {
                    foreach(string folder in folders) {
                        newContent += $"Project(\"{{2150E333-8FDC-42A3-9474-1A3956D46DE8}}\") = \"{folder}\", \"{folder}\", \"{{{folderGuids[folder]}}}\"\nEndProject\n";
                    }
                    setup = true;
                } else {
                    foreach(string mod in folderMap.Keys) {
                        if(line.Contains($"\"{mod}\"")) {
                            modGuids[mod] = line.Substring(69 + (mod.Length * 2), 36);
                        }
                    }
                }
            } else {
                if(line == "EndGlobal") {
                    newContent += "\tGlobalSection(NestedProjects) = preSolution\n";

                    foreach(string mod in folderMap.Keys) {
                        try {
                            newContent += $"\t\t{{{modGuids[mod]}}} = {{{folderGuids[folderMap[mod]]}}}\n";
                        } catch(Exception e) { UnityEngine.Debug.LogError($"{mod}\n{e}"); }
                    }

                    newContent += "\tEndGlobalSection\n";
                }
            }
            newContent += line + "\n";
        }

        return newContent;
    }
}