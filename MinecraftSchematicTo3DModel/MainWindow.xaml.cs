using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.IO;
using System.Windows.Controls;
using fNbt;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.CodeDom;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace MinecraftSchematicTo3DModel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        private string[] FilePaths { get; set; }
        private string ExportPath { get; set; }
        private string RootPath { get; set; }
        private string ModelExportPath { get; set; }
        private string TextureExportPath { get; set; }
        private string ItemExportPath { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.InitialDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    FilePaths = Directory.GetFiles(dialog.FileName);
                    pathFiles.Items.Clear();

                    // Set all output paths
                    ExportPath = dialog.FileName + "\\out";
                    RootPath = ExportPath + "\\Schematic To Model\\";
                    ModelExportPath = RootPath + "assets\\minecraft\\models\\schematic_to_model\\";
                    TextureExportPath = RootPath + "assets\\minecraft\\textures\\schematic_to_model\\";
                    ItemExportPath = RootPath + "assets\\minecraft\\models\\item\\";

                    // Create Directories
                    Directory.CreateDirectory(ModelExportPath);
                    Directory.CreateDirectory(ModelExportPath);
                    Directory.CreateDirectory(TextureExportPath);
                    Directory.CreateDirectory(ItemExportPath);

                    foreach (string path in FilePaths)
                    {
                        if (path.Contains(".schematic") || path.Contains(".schem"))
                        {
                            pathFiles.Items.Add(Path.GetFileName(path));
                        }
                    }

                    // Generate all the models
                    GenerateAllModels();
                }
                if (!pathFiles.HasItems)
                    System.Windows.MessageBox.Show("The selected folder does not contain any .schematic files!", "File not found!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateAllModels()
        {
            if (FilePaths == null)
            {
                System.Windows.MessageBox.Show("You have not selected any schematics!", "Files not found!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (string path in FilePaths)
            {
                List<String> blockNames3D = GetBlockNames3D(path);

                if (blockNames3D.Count != 0)
                    GenerateModelFile(blockNames3D, Path.GetFileNameWithoutExtension(path));
            }

            // Copy RP dependencies to output
            Copy(@"Resources/textures/schematic_to_model", TextureExportPath);
            File.Copy(@"Resources/pack.mcmeta", RootPath + "\\pack.mcmeta", true);

            if (IncludeStickBool.IsChecked.Value)
                File.Copy(@"Resources/models/item/stick.json", ItemExportPath + "stick.json", false);

            // Report all converted files to user
            string report = string.Join(Environment.NewLine, FilePaths.Select(array => string.Join("\n", Path.GetFileNameWithoutExtension(array))));
            System.Windows.MessageBox.Show("Successfully converted the following schematics:\n\n" + report, "Success!", MessageBoxButton.OK, MessageBoxImage.Information);

            Process.Start(ExportPath);
        }

        private void GenerateModelFile(List<String> blockNames3D, string fileName)
        {
            Dictionary<string, string> textures = new Dictionary<string, string>();
            List<Dictionary<string, object>> elements = new List<Dictionary<string, object>>();

            int index = 0;
            for (int y = 0; y < 16; y++)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        String blockName = blockNames3D[index];

                        // Block ID is not air
                        if (blockName != "air" && Data.ValidBlocks.Contains(blockName))
                        {
                            if (!textures.ContainsKey(blockName))
                            {
                                textures.Add(blockName, "schematic_to_model/" + blockName);
                            }

                            elements.Add(
                                new Dictionary<string, object>()
                                {
                                    { "from",  new int[] { x, y, z } },
                                    { "to",    new int[] { x+1, y+1, z+1 } },
                                    { "faces", new Dictionary<string, object>()
                                        {
                                            { "down",  new Dictionary<string, string>() { {"texture", "#" + blockName }, {"cullface", "down"} } },
                                            { "up",    new Dictionary<string, string>() { {"texture", "#" + blockName }, {"cullface", "up"} } },
                                            { "north", new Dictionary<string, string>() { {"texture", "#" + blockName }, {"cullface", "north"} } },
                                            { "south", new Dictionary<string, string>() { {"texture", "#" + blockName }, {"cullface", "south"} } },
                                            { "east",  new Dictionary<string, string>() { {"texture", "#" + blockName }, {"cullface", "east"} } },
                                            { "west",  new Dictionary<string, string>() { {"texture", "#" + blockName }, {"cullface", "west"} } },
                                        }
                                    }
                                }
                            );
                        }

                        index++;
                    }
                }
            }

            Model model = new Model(textures, elements);
            string output = JsonConvert.SerializeObject(model);

            // Create file
            FileStream fileStream = new FileStream(ModelExportPath + fileName + ".json", FileMode.Create);

            // Write output to file
            byte[] info = new UTF8Encoding(true).GetBytes(output);
            fileStream.Write(info, 0, info.Length);
            fileStream.Close();
        }

        private static List<String> GetBlockNames3D(string filePath)
        {
            // Open schematic file as NBTFile and get root tag
            NbtFile model = new NbtFile();
            model.LoadFromFile(filePath);
            var compoundTag = model.RootTag;

            // Check if schematic is 16 x 16 x 16
            short length = compoundTag.Get<NbtShort>("Length").ShortValue;
            short width = compoundTag.Get<NbtShort>("Width").ShortValue;
            short height = compoundTag.Get<NbtShort>("Height").ShortValue;

            if ((length == width) && (length == height) && (length == 16))
            {
                // Get NBTByteArray of blockIDs and metadata (in bytes)
                NbtByteArray blocksNbt = compoundTag.Get<NbtByteArray>("BlockData");
                NbtCompound paletteNbt = compoundTag.Get<NbtCompound>("Palette");

                // Convert ByteArray into int array to be iterated over
                int[] blockPositions = blocksNbt.Value.Select(x => (int)x).ToArray();

                // Convert NBTCompound → Dictionary for iteration
                Dictionary<int, string> palette = new Dictionary<int, string>();
                foreach (NbtTag tag in paletteNbt.Tags)
                {
                    // Remove namespace and blockdata i.e. 'minecraft:grass[snowy=false]' → grass'
                    String newName = tag.Name.Replace("minecraft:", "");
                    newName = Regex.Replace(newName, "\\[.+\\]", "");
                    palette.Add(tag.IntValue, newName);
                }

                // Maps palette to block positions
                // Creates a 2D Array to be broken down using triple YZX loop in the GenerateModel() Function.
                List<String> blockNames3D = new List<String>();
                foreach (int paletteID in blockPositions)
                {
                    if (palette.TryGetValue(paletteID, out string blockName))
                    {
                        blockNames3D.Add(blockName);
                    }
                }

                return blockNames3D;
            }
            else
            {
                System.Windows.MessageBox.Show("Schematic file is not 16x16x16!", "Incorrect Size Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void Copy(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                try
                {
                    File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));
                }
                catch (System.IO.IOException)
                {
                    return;
                }
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                try
                {
                    Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
                }
                catch (System.IO.IOException)
                {
                    return;
                }
            }
        }
    }
}
