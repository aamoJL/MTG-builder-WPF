using Microsoft.Win32;
using MTG;
using MTG.Scryfall;
using Newtonsoft.Json;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace MTG_builder
{
    /// <summary>
    /// Functions that uses files
    /// </summary>
    internal class IO
    {
        public static readonly string CollectionsPath = "Resources/Collections/";
        public static readonly string SetIconPath = "SetIcons/";
        public static readonly string ResourcesPath = "Resources/";
        public static readonly string SetListsFileName = "Scryfall_sets.json";
        public static readonly string CardImagePath = "Resources/CardImages/";

        /// <summary>
        /// Create directories if they do not exist
        /// </summary>
        public static void InitDirectories()
        {
            _ = Directory.CreateDirectory(CollectionsPath);
            _ = Directory.CreateDirectory(SetIconPath);
            _ = Directory.CreateDirectory(CardImagePath);
        }

        /// <summary>
        /// Returns list of collection cards from a file
        /// </summary>
        /// <param name="path">Full path to the json file</param>
        public static List<CollectionCard> ReadCollectionFromFile(string path)
        {
            using StreamReader file = File.OpenText(path);
            JsonSerializer serializer = new();
            return (List<CollectionCard>)serializer.Deserialize(file, typeof(List<CollectionCard>));
        }

        /// <summary>
        /// Returns list of card sets from a file
        /// </summary>
        public static List<CardSet> GetCardSets()
        {
            try
            {
                string path = $"{ResourcesPath}{SetListsFileName}";
                string json = File.ReadAllText(path);

                CardSetData setBase = JsonConvert.DeserializeObject<CardSetData>(json);
                return setBase.Data;
            }
            catch (IOException)
            {
                return new List<CardSet>();
            }
        }

        /// <summary>
        /// Returns array of json file names from a path
        /// </summary>
        public static string[] GetJsonFileNames(string path)
        {
            string[] files = Directory.GetFiles(path, "*.json");
            string[] fileNames = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                fileNames[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            return fileNames;
        }

        /// <summary>
        /// Downloads and saves set list file using Scryfall API
        /// </summary>
        public static void UpdateSetLists()
        {
            using WebClient client = new();
            client.DownloadFile(ScryfallAPI.SetListsUrl, $"{ResourcesPath}{SetListsFileName}");
        }

        /// <summary>
        /// Saves given json object to a file
        /// </summary>
        public static void SaveJsonToFile(string path, object jsonObject)
        {
            using StreamWriter file = File.CreateText(path);
            JsonSerializer serializer = new();
            serializer.Serialize(file, jsonObject);
        }

        /// <summary>
        /// Downloads card set icons using Scryfall API and converts them from SVG to PNG files
        /// </summary>
        public static void DownloadAndConvertSetIconSVGs()
        {
            List<CardSet> cardsets = GetCardSets();
            foreach (CardSet set in cardsets)
            {
                string path = $"{SetIconPath}{set.Code}.png";
                if (!File.Exists(path))
                {
                    if (DownloadFile($"{SetIconPath}temp.svg", set.IconSvgUri))
                    {
                        SvgDocument svgDocument = SvgDocument.Open($"{SetIconPath}temp.svg");
                        svgDocument.Width = 32;
                        svgDocument.Height = 32;
                        using System.Drawing.Bitmap smallBitmap = svgDocument.Draw();

                        try
                        {
                            smallBitmap.Save(path, ImageFormat.Png);
                        }
                        catch (Exception) { }
                    }
                }
            }
        }

        /// <summary>
        /// Downloads file from url to path
        /// </summary>
        /// <returns>True if the file was downloaded</returns>
        public static bool DownloadFile(string path, string url)
        {
            using WebClient webClient = new();
            try
            {
                webClient.DownloadFile(url, Path.GetFullPath(path));
                return true;
            }
            catch (WebException)
            {
                return false;
                throw;
            }
        }

        public static async Task DownloadFileAsync(string path, string url)
        {
            try
            {
                using WebClient webClient = new();
                await webClient.DownloadFileTaskAsync(new Uri(url), path);
            }
            catch (WebException)
            {
                throw;
            }
        }

        #region Dialogs
        public static OpenFileDialog OpenFileDialog(string relativePath)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "Text files (*.json)|*.json|All files (*.*)|*.*";
            string CombinedPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            openFileDialog.InitialDirectory = Path.GetFullPath(CombinedPath);
            return openFileDialog;
        }

        public static SaveFileDialog SaveFileDialog(string relativePath, string defaultName)
        {
            // Configure save file dialog box
            SaveFileDialog dialog = new();
            dialog.FileName = defaultName; // Default file name
            dialog.DefaultExt = ".json"; // Default file extension
            dialog.InitialDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));
            dialog.Filter = "Text documents (.json)|*.json"; // Filter files by extension
            return dialog;
        }

        public static MessageBoxResult UnsavedChangesDialog(string message = "Do you want to save changes?")
        {
            // Ask if user wants to save last collection
            string messageBoxText = message;
            string caption = "Save?";
            MessageBoxButton button = MessageBoxButton.YesNoCancel;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBoxResult result;
            result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
            return result;
        }
        #endregion
    }
}
