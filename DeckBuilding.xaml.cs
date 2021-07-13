using Microsoft.Win32;
using MTG.Scryfall;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using MTG_builder;
using System.Net;
using Svg;
using System.Drawing.Imaging;

namespace MTG
{
    /// <summary>
    /// Interaction logic for DeckBuilding.xaml
    /// </summary>
    public partial class DeckBuilding : Window
    {
        private string selectedCollectionPath = "";
        private readonly CardCollection primaryCardCollection = new();
        private readonly CardCollection secondaryCardCollection = new();

        public DeckBuilding()
        {
            InitializeComponent();

            _ = Directory.CreateDirectory(IO.CollectionsPath);
            _ = Directory.CreateDirectory(IO.SetIconPath);

            IO.UpdateSetLists();

            CollectionListBox.ItemsSource = primaryCardCollection.Cards;
            CardCollectionImageListBox.ItemsSource = secondaryCardCollection.Cards;

            // Get card sets from a file and add them to combobox
            CardSetComboBox.ItemsSource = IO.GetCardSets();

            // Get collection names from a file and add them to combobox
            CardCollectionComboBox.ItemsSource = IO.GetCollectionNames(IO.CollectionsPath);

            CardSetTypeComboBox.ItemsSource = CardSet.GetSetTypes();
            CardSetTypeComboBox.SelectedIndex = 1;
        }

        private void AddCardToCollection(Card card)
        {
            primaryCardCollection.AddCard(card);
        }
        private void AddCardToCollection(CollectionCard card, int count = 1)
        {
            primaryCardCollection.AddCard(card, count);
        }
        private void RemoveCardFromCollection(CollectionCard card)
        {
            primaryCardCollection.RemoveCard(card);
        }
        private void SaveCollection(string path)
        {
            if (primaryCardCollection.Name == "")
            {
                // Create new collection if collections has not been selected
                CopyCollection();
            }
            else
            {
                primaryCardCollection.Save(path);
            }
        }
        private void ChangeCollection(string path)
        {
            List<CollectionCard> cards = IO.ReadCollectionFromFile(path);
            string collectionsName = Path.GetFileNameWithoutExtension(path);

            primaryCardCollection.ChangeCollection(cards, collectionsName);
            CollectionTextBlock.Text = collectionsName;
            selectedCollectionPath = path;
        }

        private void SetCollectionTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CollectionTab.IsSelected)
            {
                CardCollectionComboBox.ItemsSource = IO.GetCollectionNames(IO.CollectionsPath);
            }
        }
        private void CardSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(CardSetComboBox.SelectedIndex == -1) { return; }
            CardSet set = CardSetComboBox.SelectedItem as CardSet;

            List<Card> cards = IO.FetchScryfallSetCards(set.SearchUri);
            CardSetImageListBox.ItemsSource = cards;
        }
        private void CardCollectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(CardCollectionComboBox.SelectedIndex == -1) { return; }
            string collectionName = CardCollectionComboBox.SelectedItem.ToString();

            List<CollectionCard> collectionCards = IO.ReadCollectionFromFile($"{IO.CollectionsPath}{collectionName}.json");

            if (collectionCards != null)
            {
                secondaryCardCollection.ChangeCollection(collectionCards, collectionName);
                //CardCollectionImageListBox.ItemsSource = collectionCards;
            }
            else
            {
                secondaryCardCollection.Clear();
                //CardCollectionImageListBox.ItemsSource = null;
            }
        }
        private void CardSetTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CardSetComboBox.ItemsSource = FilterCardSetList(IO.GetCardSets(), new CardSet.CardSetType[] { (CardSet.CardSetType)CardSetTypeComboBox.SelectedItem });
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
        private void CollectionCardImage_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //Only double left click
            if (e.ChangedButton != MouseButton.Left) { return; }

            if ((sender as ListBoxItem).DataContext is CollectionCard collectionCard)
            {
                AddCardToCollection(collectionCard);
            }
            else if((sender as ListBoxItem).DataContext is Card card)
            {
                AddCardToCollection(card);
            }
        }
        private void CardImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image img)
            {
                if(img.DataContext is Card card)
                {
                    if (card.HasTwoFaces)
                    {
                        string currentSource = img.Source.ToString();
                        img.Source = card.CardFaces[0].ImageUris["normal"] == currentSource ? card.SecondaryFace : card.PrimaryFace;
                    }
                }
                else if(img.DataContext is CollectionCard collectionCard)
                {
                    if (collectionCard.Card.HasTwoFaces)
                    {
                        string currentSource = img.Source.ToString();
                        img.Source = collectionCard.Card.CardFaces[0].ImageUris["normal"] == currentSource ? collectionCard.Card.SecondaryFace : collectionCard.Card.PrimaryFace;
                    }
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
            if (primaryCardCollection.UnsavedChanges)
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
            string CombinedPath = Path.Combine(Directory.GetCurrentDirectory(), IO.CollectionsPath);
            openFileDialog.InitialDirectory = Path.GetFullPath(CombinedPath);

            if (openFileDialog.ShowDialog() == true)
            {
                ChangeCollection(openFileDialog.FileName);
            }
        }
        private void NewCollectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (primaryCardCollection.UnsavedChanges)
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
        private void DeckTestingButton_Click(object sender, RoutedEventArgs e)
        {
            DeckTesting deckTestingWindow = new();
            deckTestingWindow.Show();
        }
        private void CollectionSwapLeftButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = CardCollectionImageListBox.SelectedIndex;
            if (selectedIndex == -1) { return; }

            SwapCardCollection(secondaryCardCollection, primaryCardCollection, selectedIndex);
        }
        private void CollectionSwapRightButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = CollectionListBox.SelectedIndex;
            if (selectedIndex == -1 || secondaryCardCollection.Name == "") { return; }

            SwapCardCollection(primaryCardCollection, secondaryCardCollection, selectedIndex);
        }
        private void SecondaryCollectionSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (secondaryCardCollection.Name == "") { return; }
            secondaryCardCollection.Save($"{IO.CollectionsPath}{secondaryCardCollection.Name}.json");
        }

        private void CreateNewCollection()
        {
            // Configure save file dialog box
            SaveFileDialog dialog = new();
            dialog.FileName = "NewCollection"; // Default file name
            dialog.DefaultExt = ".json"; // Default file extension
            dialog.InitialDirectory = Path.GetFullPath(IO.CollectionsPath);
            dialog.Filter = "Text documents (.json)|*.json"; // Filter files by extension

            // Show save file dialog box
            bool? result = dialog.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string path = dialog.FileName;

                primaryCardCollection.Clear();
                primaryCardCollection.Name = dialog.SafeFileName;
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
            dialog.InitialDirectory = Path.GetFullPath(IO.CollectionsPath);
            dialog.Filter = "Text documents (.json)|*.json"; // Filter files by extension

            // Show save file dialog box
            bool? result = dialog.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string path = dialog.FileName;

                primaryCardCollection.Name = dialog.SafeFileName;
                SaveCollection(path);
                ChangeCollection(path);
            }
        }

        private static void DownloadAndConvertSetIconSVGs()
        {
            List<CardSet> cardsets = IO.GetCardSets();
            foreach (CardSet set in cardsets)
            {
                string path = $"{IO.SetIconPath}{set.Code}.png";
                if (!File.Exists(path))
                {
                    using WebClient webClient = new();
                    webClient.DownloadFile(set.IconSvgUri, $"{IO.SetIconPath}temp.svg");
                    SvgDocument svgDocument = SvgDocument.Open($"{IO.SetIconPath}temp.svg");
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
        private static void SwapCardCollection(CardCollection fromCollection, CardCollection toCollection, int fromIndex)
        {
            CollectionCard card = fromCollection.Cards[fromIndex];

            toCollection.AddCard(card);
            fromCollection.RemoveCard(card);
        }

        private void UpdateMenuIconsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DownloadAndConvertSetIconSVGs();
        }
        private void UpdateSetListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IO.UpdateSetLists();

            CardSetComboBox.ItemsSource = IO.GetCardSets();
        }
    }
}