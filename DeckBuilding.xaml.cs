using Microsoft.Win32;
using MTG.Scryfall;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace MTG
{
    /// <summary>
    /// Interaction logic for DeckBuilding.xaml
    /// </summary>
    public partial class DeckBuilding : Window
    {
        private readonly string collectionsPath = "Resources/Collections/";
        private string selectedCollectionPath = "";
        private readonly CardCollection cardCollection = new();

        public DeckBuilding()
        {
            InitializeComponent();

            _ = Directory.CreateDirectory(collectionsPath);

            CollectionListBox.ItemsSource = cardCollection.Cards;

            // Get card sets from a file and add them to combobox
            CardSetComboBox.ItemsSource = GetCardSets();

            // Get collection names from a file and add them to combobox
            CardCollectionComboBox.ItemsSource = GetCollectionNames(collectionsPath);

            CardSetTypeComboBox.ItemsSource = CardSet.GetSetTypes();
            CardSetTypeComboBox.SelectedIndex = 1;
        }

        private static List<CardSet> GetCardSets()
        {
            try
            {
                string path = "Resources/Scryfall_sets.json";
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
        private static string[] GetCollectionNames(string path)
        {
            string[] files = Directory.GetFiles(path, "*.json");
            string[] fileNames = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                fileNames[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            return fileNames;
        }
        private static List<Card> GetCardsFromCollection(CardCollection collection)
        {
            List<Card> cards = new();
            foreach (CollectionCard collectionCard in collection.Cards)
            {
                cards.Add(collectionCard.Card);
            }

            return cards;
        }
        private static List<Card> GetCardsFromCollection(List<CollectionCard> collectionCards)
        {
            List<Card> cards = new();
            foreach (CollectionCard collectionCard in collectionCards)
            {
                cards.Add(collectionCard.Card);
            }
            return cards;
        }
        private static List<Card> FetchScryfallSetCards(string searchUri)
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
        private static List<CollectionCard> ReadCollectionFromFile(string path)
        {
            using StreamReader file = File.OpenText(path);
            JsonSerializer serializer = new();
            return (List<CollectionCard>)serializer.Deserialize(file, typeof(List<CollectionCard>));
        }

        private void AddCardToCollection(Card card)
        {
            cardCollection.AddCard(card);
        }
        private void AddCardToCollection(CollectionCard card)
        {
            cardCollection.AddCard(card);
        }
        private void RemoveCardFromCollection(CollectionCard card)
        {
            cardCollection.RemoveCard(card);
        }
        private void SaveCollection(string path)
        {
            if (cardCollection.Name == "")
            {
                // Create new collection if collections has not been selected
                CopyCollection();
            }
            else
            {
                cardCollection.Save(path);
            }
        }
        private void ChangeCollection(string path)
        {
            List<CollectionCard> cards = ReadCollectionFromFile(path);
            string collectionsName = Path.GetFileNameWithoutExtension(path);

            cardCollection.ChangeCollection(cards, collectionsName);
            CollectionTextBlock.Text = collectionsName;
            selectedCollectionPath = path;
        }

        private void SetCollectionTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CollectionTab.IsSelected)
            {
                CardCollectionComboBox.ItemsSource = GetCollectionNames(collectionsPath);
            }
        }
        private void CardSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(CardSetComboBox.SelectedIndex == -1) { return; }
            CardSet set = CardSetComboBox.SelectedItem as CardSet;

            List<Card> cards = FetchScryfallSetCards(set.SearchUri);
            CardSetImageListBox.ItemsSource = cards;
            CardCollectionComboBox.SelectedIndex = -1;
        }
        private void CardCollectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(CardCollectionComboBox.SelectedIndex == -1) { return; }
            string collectionName = CardCollectionComboBox.SelectedItem.ToString();

            List<CollectionCard> collectionCards = ReadCollectionFromFile($"{collectionsPath}{collectionName}.json");

            if(collectionCards != null)
            {
                List<Card> cards = GetCardsFromCollection(collectionCards);
                CardSetImageListBox.ItemsSource = cards;
                CardSetComboBox.SelectedIndex = -1;
            }
            else
            {
                CardSetImageListBox.ItemsSource = null;
            }
        }
        private void CardSetTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CardSetComboBox.ItemsSource = FilterCardSetList(GetCardSets(), new CardSet.CardSetType[] { (CardSet.CardSetType)CardSetTypeComboBox.SelectedItem });
        }
        private void CollectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CollectionSelectedImage.Source = ((CollectionCard)CollectionListBox.SelectedItem)?.Card.PrimaryFace;
        }

        private void CardImage_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //Only double left click
            if (e.ChangedButton != MouseButton.Left) { return; }

            if (CardSetImageListBox.SelectedItem is Card card)
            {
                AddCardToCollection(card);
            }
        }
        private void CardImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image img && CardSetImageListBox.SelectedItem is Card card)
            {
                if (card.HasTwoFaces)
                {
                    string currentSource = img.Source.ToString();
                    img.Source = card.CardFaces[0].ImageUris["normal"] == currentSource ? card.SecondaryFace : card.PrimaryFace;
                }
            }
        }
        private void CollectionCard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (((ListBoxItem)sender).DataContext is CollectionCard collectionCard)
            {
                CollectionHoverImage.Source = collectionCard.Card.PrimaryFace;
            }
        }
        private void CollectionCard_MouseLeave(object sender, MouseEventArgs e)
        {
            CollectionHoverImage.Source = null;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (CollectionListBox.SelectedItem is CollectionCard card)
            {
                AddCardToCollection(card);
            }
        }
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (CollectionListBox.SelectedItem is CollectionCard card)
            {
                RemoveCardFromCollection(card);
            }
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCollection(selectedCollectionPath);
        }
        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            CopyCollection();
        }
        private void OpenCollectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (cardCollection.UnsavedChanges)
            {
                MessageBoxResult message = UnsavedChangesDialog();
                switch (message)
                {
                    case MessageBoxResult.Cancel:
                        return;
                    case MessageBoxResult.Yes:
                        SaveCollection(selectedCollectionPath);
                        break;
                    case MessageBoxResult.No:
                    case MessageBoxResult.None:
                    case MessageBoxResult.OK:
                    default:
                        break;
                }
            }

            OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "Text files (*.json)|*.json|All files (*.*)|*.*";
            string CombinedPath = Path.Combine(Directory.GetCurrentDirectory(), collectionsPath);
            openFileDialog.InitialDirectory = Path.GetFullPath(CombinedPath);

            if (openFileDialog.ShowDialog() == true)
            {
                ChangeCollection(openFileDialog.FileName);
            }
        }
        private void NewCollectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (cardCollection.UnsavedChanges)
            {
                MessageBoxResult message = UnsavedChangesDialog();
                switch (message)
                {
                    case MessageBoxResult.Cancel:
                        return;
                    case MessageBoxResult.Yes:
                        SaveCollection(selectedCollectionPath);
                        break;
                    case MessageBoxResult.No:
                    case MessageBoxResult.None:
                    case MessageBoxResult.OK:
                    default:
                        break;
                }
            }

            CreateNewCollection();
        }

        private void CreateNewCollection()
        {
            // Configure save file dialog box
            SaveFileDialog dialog = new();
            dialog.FileName = "NewCollection"; // Default file name
            dialog.DefaultExt = ".json"; // Default file extension
            dialog.InitialDirectory = Path.GetFullPath(collectionsPath);
            dialog.Filter = "Text documents (.json)|*.json"; // Filter files by extension

            // Show save file dialog box
            bool? result = dialog.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string path = dialog.FileName;

                cardCollection.Clear();
                cardCollection.Name = dialog.SafeFileName;
                SaveCollection(path);
                ChangeCollection(path);
            }
        }
        private void CopyCollection()
        {
            // Configure save file dialog box
            SaveFileDialog dialog = new();
            dialog.FileName = "CopiedCollection"; // Default file name
            dialog.DefaultExt = ".json"; // Default file extension
            dialog.InitialDirectory = Path.GetFullPath(collectionsPath);
            dialog.Filter = "Text documents (.json)|*.json"; // Filter files by extension

            // Show save file dialog box
            bool? result = dialog.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string path = dialog.FileName;

                cardCollection.Name = dialog.SafeFileName;
                SaveCollection(path);
                ChangeCollection(path);
            }
        }

        private static MessageBoxResult UnsavedChangesDialog()
        {
            // Ask if user wants to save last collection
            string messageBoxText = "Do you want to save changes?";
            string caption = "Save?";
            MessageBoxButton button = MessageBoxButton.YesNoCancel;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBoxResult result;

            result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);

            return result;
        }
        private static List<CardSet> FilterCardSetList(List<CardSet> setList, CardSet.CardSetType[] types)
        {
            return setList.Where(x => types.Contains(x.SetType)).ToList();
        }
    }
}