using Microsoft.Win32;
using MTG.Scryfall;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace MTG
{
    /// <summary>
    /// Interaction logic for DeckBuilding.xaml
    /// </summary>
    public partial class DeckBuilding : Window
    {
        string collectionsPath = "Resources/Collections/";
        string selectedCollectionPath = "";
        readonly CardCollection cardCollection = new CardCollection();

        public DeckBuilding()
        {
            InitializeComponent();

            Directory.CreateDirectory(collectionsPath);

            CollectionListBox.ItemsSource = cardCollection.Cards;

            // Get card sets from a file and add them to combobox
            CardSetComboBox.ItemsSource = GetCardSets();

            // Get collection names from a file and add them to combobox
            CardCollectionComboBox.ItemsSource = GetCollectionNames();

            CardSetTypeComboBox.ItemsSource = CardSet.GetSetTypes();
            CardSetTypeComboBox.SelectedIndex = 1;
        }

        private List<CardSet> GetCardSets()
        {
            try
            {
                string path = "Resources/Scryfall_sets.json";
                string json = File.ReadAllText(path);

                CardSetData setBase = JsonConvert.DeserializeObject<CardSetData>(json);
                return setBase.data;
            }
            catch (IOException e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }
        }
        private string[] GetCollectionNames()
        {
            Directory.CreateDirectory(collectionsPath);

            string[] files = Directory.GetFiles(collectionsPath, "*.json");
            string[] fileNames = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                fileNames[i] = System.IO.Path.GetFileNameWithoutExtension(files[i]);
            }

            return fileNames;
        }
        private List<Card> GetCardsFromCollection(CardCollection collection)
        {
            List<Card> cards = new List<Card>();
            foreach (CollectionCard collectionCard in collection.Cards)
            {
                cards.Add(collectionCard.Card);
            }

            return cards;
        }
        private List<Card> GetCardsFromCollection(List<CollectionCard> collectionCards)
        {
            List<Card> cards = new List<Card>();
            foreach (CollectionCard collectionCard in collectionCards)
            {
                cards.Add(collectionCard.Card);
            }
            return cards;
        }
        private List<Card> FetchScryfallSetCards(string searchUri)
        {
            using (WebClient wc = new WebClient())
            {
                var json = wc.DownloadString(searchUri);
                return JsonConvert.DeserializeObject<CardData>(json).data;
            }
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
        private List<CollectionCard> ReadCollectionFromFile(string path)
        {
            using (StreamReader file = File.OpenText(path))
            {
                JsonSerializer serializer = new JsonSerializer();
                return (List<CollectionCard>)serializer.Deserialize(file, typeof(List<CollectionCard>));
            }
        }
        private void ChangeCollection(string path)
        {
            List<CollectionCard> cards = ReadCollectionFromFile(path);
            string collectionsName = System.IO.Path.GetFileNameWithoutExtension(path);

            cardCollection.ChangeCollection(cards, collectionsName);
            CollectionTextBlock.Text = collectionsName;
            selectedCollectionPath = path;
        }

        private void SetCollectionTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CollectionTab.IsSelected)
            {
                CardCollectionComboBox.ItemsSource = GetCollectionNames();
            }
        }
        private void CardSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(CardSetComboBox.SelectedIndex == -1) { return; }
            CardSet set = CardSetComboBox.SelectedItem as CardSet;

            List<Card> cards = FetchScryfallSetCards(set.search_uri);
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
            CardSetComboBox.ItemsSource = FilterCardSetList(GetCardSets(), new CardSet.SetType[] { (CardSet.SetType)CardSetTypeComboBox.SelectedItem });
        }
        private void CollectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CollectionSelectedImage.Source = ((CollectionCard)CollectionListBox.SelectedItem)?.Card.PrimaryFace;
        }

        private void CardImage_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton != MouseButton.Left) { return; }
            
            //Only left double click
            Card card = CardSetImageListBox.SelectedItem as Card;

            if (card != null)
            {
                AddCardToCollection(card);
            }
        }
        private void CardImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Image img = sender as Image;
            Card card = CardSetImageListBox.SelectedItem as Card;

            if (img != null && card != null)
            {
                if (card.HasTwoFaces)
                {
                    string currentSource = img.Source.ToString();
                    if(card.card_faces[0].image_uris["normal"] == currentSource)
                    {
                        img.Source = card.SecondaryFace;
                    }
                    else
                    {
                        img.Source = card.PrimaryFace;
                    }
                }
            }
        }
        private void CollectionCard_MouseEnter(object sender, MouseEventArgs e)
        {
            CollectionCard collectionCard = ((ListBoxItem)sender).DataContext as CollectionCard;

            if (collectionCard != null)
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
            CollectionCard card = CollectionListBox.SelectedItem as CollectionCard;

            if (card != null)
            {
                AddCardToCollection(card);
            }
        }
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            CollectionCard card = CollectionListBox.SelectedItem as CollectionCard;

            if (card != null)
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
                        break;
                    default:
                        break;
                }
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.json)|*.json|All files (*.*)|*.*";
            string CombinedPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), collectionsPath);
            openFileDialog.InitialDirectory = System.IO.Path.GetFullPath(CombinedPath);

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
                        break;
                    default:
                        break;
                }
            }

            CreateNewCollection();
        }

        private void CreateNewCollection()
        {
            // Configure save file dialog box
            SaveFileDialog dialog = new SaveFileDialog();
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
            var dialog = new SaveFileDialog();
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

        private MessageBoxResult UnsavedChangesDialog()
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

        private List<CardSet> FilterCardSetList(List<CardSet> setList, CardSet.SetType[] types)
        {
            return setList.Where(x => types.Contains(x.set_type)).ToList();
        }
    }
}