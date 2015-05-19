using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Markup;
using System.Reflection;
using System.Xml;

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;


namespace Company.MultiSlotClipboard
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class MyControl : UserControl
    {
        private const int MAX_SLOTS = 5;
        private const int MAX_SLOT_HISTORY = 10;

        private IWpfTextView view;

        private List<String>[] aClipboadSlotData = new List<String>[MAX_SLOTS];
        private Grid[] aGroupGrids = new Grid[MAX_SLOTS];


        public MyControl()
        {
            InitializeComponent();

            // Add the empty slots
            IntilializeClipBoard();
            
            // Initilizae the interface
            InitializeInterface();
        }

        // Sets the initial values of the data structure aClipboadSlotData
        private void IntilializeClipBoard()
        {
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                aClipboadSlotData[i] = new List<string>();


                for (int j = 0; j < MAX_SLOT_HISTORY; j++)
                {
                    aClipboadSlotData[i].Insert(j, "");
                }
            }
        }


        private void InitializeInterface()
        {

            Double left_initial = -170;

            this.grdTemplate.Visibility = Visibility.Hidden;

            for (int i = 0; i < MAX_SLOTS; i++)
            {
                aGroupGrids[i] = CloneControls(this.grdTemplate);
                aGroupGrids[i].Visibility = Visibility.Visible;

                left_initial = left_initial + 180.0;
                aGroupGrids[i].Margin = new System.Windows.Thickness(left_initial, -5.0, 0.0, 0.0);

                // Need to change the Slot label + add the events to the buttons and the list
                // Order of elementos
                // 0 -> Button Paste
                // 1 -> Button Copy
                // 2 -> ListView
                // 3 -> Label
                // 4 -> Button Cut


                aGroupGrids[i].Tag = i;

                // Set the Label caption
                ((Label)aGroupGrids[i].Children[3]).Content = "Slot " + (i + 1).ToString();

                // Set the Tags properties with the index of this grid in the array
                // This is gonna be usefull latter on (for the events)

                Button Paste = ((Button)aGroupGrids[i].Children[0]);
                Button Copy = ((Button)aGroupGrids[i].Children[1]);
                Button Cut = ((Button)aGroupGrids[i].Children[4]);


                ListView lstData = ((ListView)aGroupGrids[i].Children[2]);

                Paste.Tag = i;
                Copy.Tag = i;
                Cut.Tag = i;
                lstData.Tag = i;

                // Set the Buttons Events (Click)
                Copy.Click += Copy_Click;
                Paste.Click += Paste_Click;
                Cut.Click += Cut_Click;


                // Set the ListView Event (MouseDoubleClick)
                lstData.MouseDoubleClick += lstData_MouseDoubleClick;

                // Add this grid to the parent Grid
                grdMain.Children.Add(aGroupGrids[i]);
            }

        }

        private IWpfTextView GetActiveTextView()
        {
            IWpfTextView view = null;
            IVsTextView vTextView;
            var txtMgr = MultiSlotClipboardPackage.thePackage.getTextManager();
            
            txtMgr.GetActiveView(1, null, out vTextView);

            var userData = vTextView as IVsUserData;

            if (null != userData)
            {
                object holder;

                var guidViewHost = DefGuidList.guidIWpfTextViewHost;
                userData.GetData(ref guidViewHost, out holder);

                var viewHost = (IWpfTextViewHost)holder;
                view = viewHost.TextView;
            }

            return view;
        }


        // Get the SelectedText
        private static string GetTextForPastie(ITextView view)
        {
            if (SelectionIsAvailable(view))
                return GetSelectedText(view);
            else
                return "";
        }

        // Check to see if there is selected text
        private static bool SelectionIsAvailable(ITextView view)
        {
            if (view == null)
                throw new ArgumentNullException("view");

            return !view.Selection.IsEmpty && view.Selection.SelectedSpans.Count > 0;
        }

        // Get the current selected text
        private static string GetSelectedText(ITextView view)
        {
            return view.Selection.SelectedSpans[0].GetText();
        }


        private Grid CloneControls(Grid myGrid)
        {
            string gridXaml = XamlWriter.Save(myGrid);

            StringReader stringReader = new StringReader(gridXaml);

            XmlReader xmlReader = XmlReader.Create(stringReader);
            Grid newGrid = (Grid)XamlReader.Load(xmlReader);

            return newGrid;
        }

        void lstData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListView v = (ListView)sender;

            int index = Convert.ToInt32(v.Tag);

            if (v.SelectedIndex != -1)
                MessageBox.Show(aClipboadSlotData[index].ElementAt(v.SelectedIndex).ToString());
        }

        void Copy_Click(object sender, RoutedEventArgs e)
        {
            int index = Convert.ToInt32(((Button)sender).Tag);

            view = GetActiveTextView();
            if (!(this.view == null ||  MultiSlotClipboardPackage.thePackage.Dte.ActiveDocument == null))
            {
                String sData = GetTextForPastie(view);

                if (sData != "")
                {
                    InsertDataHistory(index, sData);
                    AccomodateDataListView(index);
                }

            }
        }

        void Cut_Click(object sender, RoutedEventArgs e)
        {
            int index = Convert.ToInt32(((Button)sender).Tag);

            view = GetActiveTextView();
            if (!(this.view == null || MultiSlotClipboardPackage.thePackage.Dte.ActiveDocument == null))
            {
                String sData = GetTextForPastie(view);

                if (sData != "")
                {
                    InsertDataHistory(index, sData);

                    AccomodateDataListView(index);

                    Microsoft.VisualStudio.Text.ITextEdit edit = view.TextBuffer.CreateEdit();
                    edit.Delete(view.Selection.Start.Position.Position, view.Selection.End.Position.Position - view.Selection.Start.Position.Position);
                    edit.Apply();

                    view.Selection.Clear();
                }
            }
        }

        void Paste_Click(object sender, RoutedEventArgs e)
        {
            int index = Convert.ToInt32(((Button)sender).Tag);

            view = GetActiveTextView();
            if (!(this.view == null || MultiSlotClipboardPackage.thePackage.Dte.ActiveDocument == null))
            {
                String sData = getSelectedItem(index);

                if (!sData.Equals(""))
                {
                    Microsoft.VisualStudio.Text.ITextEdit edit = view.TextBuffer.CreateEdit();
                    edit.Insert(view.Caret.Position.BufferPosition.Position, sData);
                    edit.Apply();
                }
            }
        }


        private String getSelectedItem(int indexSlot)
        {
            String ret = "";
            ListView v = (ListView)aGroupGrids[indexSlot].Children[2];

            if (v.Items.Count > 0)
            {
                if (v.SelectedIndex >= 0)
                {
                    ret = aClipboadSlotData[indexSlot].ElementAt(v.SelectedIndex).ToString();
                }
                else
                    ret = aClipboadSlotData[indexSlot].ElementAt(0).ToString();
            }

            return ret;
        }



        private void InsertDataHistory(int indexSlot, String sData)
        {

            List<String> list_temp = new List<String>();

            list_temp.Insert(0, sData);

            for (int i = 0; i < aClipboadSlotData[indexSlot].Count - 1; i++)
            {
                list_temp.Insert(i + 1, aClipboadSlotData[indexSlot].ElementAt(i).ToString());
            }

            aClipboadSlotData[indexSlot].RemoveRange(0, aClipboadSlotData[indexSlot].Count);
            aClipboadSlotData[indexSlot].InsertRange(0, list_temp);

            return;
        }

        private void AccomodateDataListView(int indexSlot)
        {
            ListView v = (ListView)aGroupGrids[indexSlot].Children[2];

            v.Items.Clear();
            foreach (String s in aClipboadSlotData[indexSlot])
            {
                if (!s.Equals(""))
                {
                    if (s.Length > 15)
                        v.Items.Add(s.Substring(0, 15));
                    else
                        v.Items.Add(s);
                }
            }

            v.Items.Refresh();
            v.SelectedIndex = 0;
        }
    }
}