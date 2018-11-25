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

namespace SchematicToTrophy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        string selectedFolderPath;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog() { Description = "Select your path." })
            {
                fbd.SelectedPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                DialogResult result = fbd.ShowDialog();
                
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string[] filePaths = Directory.GetFiles(fbd.SelectedPath);
                    selectedFolderPath = fbd.SelectedPath;

                    pathFiles.Items.Clear();

                    foreach (string path in filePaths)
                    {
                        string fileName = Path.GetFileName(path.ToString());

                        if (fileName.Contains(".schematic"))
                            pathFiles.Items.Add(fileName);
                    }
                }

                if (!pathFiles.HasItems)
                    System.Windows.MessageBox.Show("Selected folder does not have any .schematic files!", "File not found!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateModelClick(object sender, RoutedEventArgs e)
        {
            if (pathFiles.SelectedItem != null && selectedFolderPath != "") {
                var blockIDArray = GetBlockIDArray(selectedFolderPath + "/" + pathFiles.SelectedItem.ToString());

                if(blockIDArray != null)
                    GenerateModelFile(blockIDArray, selectedFolderPath + "/" + pathFiles.SelectedItem.ToString());
            }
            else
                System.Windows.MessageBox.Show("No file was selected in list!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void GenerateModelFile(List<int> blockIDs, string filePath)
        {
            Dictionary<string, string> textures = new Dictionary<string, string>();
            List<Dictionary<string, object>> elements = new List<Dictionary<string, object>>();

            Data data = new Data();

            int index = 0;
            for (int y = 0; y < 16; y++)
            {
                for (int z = 0; z < 16; z++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        int blockID = blockIDs[index];

                        if (blockID != 0)
                        {
                            string blockName = data.texturePaths[blockID];

                            if (!textures.ContainsKey(blockName))
                            {
                                textures.Add(blockName, "block/custom/" + blockName);
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
            string fileName = Path.GetFileName(filePath);
            fileName = fileName.Replace(".schematic", "");
            FileStream fileStream = new FileStream(selectedFolderPath + "/" + fileName + ".json", FileMode.Create);

            // Write output to file
            byte[] info = new UTF8Encoding(true).GetBytes(output);
            fileStream.Write(info, 0, info.Length);
            fileStream.Close();

            System.Windows.MessageBox.Show("Model file successfully generated at:\n\n" + selectedFolderPath, "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static List<int> GetBlockIDArray(string filePath)
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
                System.Windows.MessageBox.Show("Size of schematic: " + length + " " + width + " " + height);

                // Get NBTByteArray of blockIDs and metadata (in bytes)
                NbtByteArray blocks = compoundTag.Get<NbtByteArray>("Blocks");
                NbtByteArray data = compoundTag.Get<NbtByteArray>("Data");

                // Convert ByteArrays into int arrays to get blockIDs and metadata
                int[] blockIDs = blocks.Value.Select(x => (int)x).ToArray();
                int[] blockDatas = data.Value.Select(x => (int)x).ToArray();

                List<int> blockIDAndData = new List<int>();

                // Create custom integers based on blockIDs and metadata
                // blockID + (blockData * 4096)
                for (int i = 0; i < blockIDs.Length; i++)
                {
                    int blockID = blockIDs[i];
                    int blockData = blockDatas[i];

                    blockIDAndData.Add(blockID + (blockData * 4096));
                }

                return blockIDAndData;
            }
            else
            {
                System.Windows.MessageBox.Show("Schematic file is not 16x16x16!", "Incorrect Size Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}
