using Microsoft.Win32;
using MTG.Scryfall;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace MTG_builder
{
    internal class IO
    {
        public static readonly string CollectionsPath = "Resources/Collections/";
        public static readonly string SetIconPath = "SetIcons/";
        public static readonly string ResourcesPath = "Resources/";
        public static readonly string SetListsFileName = "Scryfall_sets.json";

        public static List<CollectionCard> ReadCollectionFromFile(string path)
        {
            using StreamReader file = File.OpenText(path);
            JsonSerializer serializer = new();
            return (List<CollectionCard>)serializer.Deserialize(file, typeof(List<CollectionCard>));
        }
        public static List<Card> FetchScryfallSetCards(string searchUri)
        {
            List<Card> cards = new();

            using WebClient wc = new();

            string json = wc.DownloadString(searchUri);
            CardData cardData = JsonConvert.DeserializeObject<CardData>(json);
            cards.AddRange(cardData.Data);

            while (cardData.HasMore)
            {
                json = wc.DownloadString(cardData.NextPage);
                cardData = JsonConvert.DeserializeObject<CardData>(json);
                cards.AddRange(cardData.Data);
            }

            return cards;
        }
        public static List<CardSet> GetCardSets()
        {
            try
            {
                string path = $"{ResourcesPath}{SetListsFileName}";
                string json = File.ReadAllText(path);

                CardSetData setBase = JsonConvert.DeserializeObject<CardSetData>(json);
                return setBase.Data;
            }
            catch (IOException e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }
        }
        public static string[] GetCollectionNames(string path)
        {
            string[] files = Directory.GetFiles(path, "*.json");
            string[] fileNames = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                fileNames[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            return fileNames;
        }
        public static List<Card> GetCardsFromCollection(CardCollection collection)
        {
            List<Card> cards = new();
            foreach (CollectionCard collectionCard in collection.Cards)
            {
                cards.Add(collectionCard.Card);
            }

            return cards;
        }
        public static List<Card> GetCardsFromCollection(List<CollectionCard> collectionCards)
        {
            List<Card> cards = new();
            foreach (CollectionCard collectionCard in collectionCards)
            {
                cards.Add(collectionCard.Card);
            }
            return cards;
        }
        public static void UpdateSetLists()
        {
            using WebClient client = new();
            client.DownloadFile(Scryfall.SetListsUrl, $"{ResourcesPath}{SetListsFileName}");
        }

        public static OpenFileDialog OpenFileDialog(string relativePath)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "Text files (*.json)|*.json|All files (*.*)|*.*";
            string CombinedPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            openFileDialog.InitialDirectory = Path.GetFullPath(CombinedPath);
            return openFileDialog;
        }
    }
}
