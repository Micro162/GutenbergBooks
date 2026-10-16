
using System;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;

namespace GutenbergBooks
{
    public partial class Form1 : Form
    {
        HttpClient http = new HttpClient();

        TabControl tabs = new TabControl();

        TextBox hamletText = new TextBox();
        TextBox booksText = new TextBox();
        TextBox searchText = new TextBox();
        TextBox query = new TextBox();

        ListBox popularList = new ListBox();
        ListBox searchList = new ListBox();

        public Form1()
        {
            InitializeComponent();

            Text = "Бібліотека Gutenberg";
            Width = 1000;
            Height = 700;

            tabs.Dock = DockStyle.Fill;
            Controls.Add(tabs);

            TabPage page1 = new TabPage("Гамлет");
            TabPage page2 = new TabPage("Популярні книги");
            TabPage page3 = new TabPage("Пошук книг");

            tabs.TabPages.Add(page1);
            tabs.TabPages.Add(page2);
            tabs.TabPages.Add(page3);

            Button hamletButton = new Button();
            hamletButton.Text = "Завантажити Гамлета";
            hamletButton.SetBounds(10, 10, 200, 35);
            hamletButton.Click += LoadHamlet;

            hamletText.SetBounds(10, 55, 950, 570);
            hamletText.Multiline = true;
            hamletText.ScrollBars = ScrollBars.Vertical;
            hamletText.ReadOnly = true;

            page1.Controls.Add(hamletButton);
            page1.Controls.Add(hamletText);

            Button popularButton = new Button();
            popularButton.Text = "Завантажити 100 книг";
            popularButton.SetBounds(10, 10, 200, 35);
            popularButton.Click += LoadPopular;

            popularList.SetBounds(10, 55, 300, 570);
            booksText.SetBounds(320, 55, 640, 570);
            booksText.Multiline = true;
            booksText.ScrollBars = ScrollBars.Vertical;
            booksText.ReadOnly = true;

            popularList.SelectedIndexChanged += async (s, e) =>
            {
                if (popularList.SelectedItem == null) return;

                Book book = (Book)popularList.SelectedItem;
                booksText.Text = await GetBookText(book.Url);
            };

            page2.Controls.Add(popularButton);
            page2.Controls.Add(popularList);
            page2.Controls.Add(booksText);

            query.SetBounds(10, 10, 300, 30);

            Button searchButton = new Button();
            searchButton.Text = "Шукати";
            searchButton.SetBounds(320, 10, 100, 30);
            searchButton.Click += SearchBooks;

            searchList.SetBounds(10, 50, 300, 575);
            searchText.SetBounds(320, 50, 640, 575);
            searchText.Multiline = true;
            searchText.ScrollBars = ScrollBars.Vertical;
            searchText.ReadOnly = true;

            searchList.SelectedIndexChanged += async (s, e) =>
            {
                if (searchList.SelectedItem == null) return;

                Book book = (Book)searchList.SelectedItem;
                searchText.Text = await GetBookText(book.Url);
            };

            page3.Controls.Add(query);
            page3.Controls.Add(searchButton);
            page3.Controls.Add(searchList);
            page3.Controls.Add(searchText);
        }

        async void LoadHamlet(object sender, EventArgs e)
        {
            try
            {
                hamletText.Text = await http.GetStringAsync(
                    "https://www.gutenberg.org/files/1524/1524-0.txt");
            }
            catch
            {
                hamletText.Text = "Помилка завантаження.";
            }
        }

        async void LoadPopular(object sender, EventArgs e)
        {
            try
            {
                popularList.Items.Clear();

                for (int page = 1; page <= 4; page++)
                {
                    string json = await http.GetStringAsync(
                        "https://gutendex.com/books/?sort=popular&page=" + page);

                    using JsonDocument doc = JsonDocument.Parse(json);

                    foreach (JsonElement item in
                        doc.RootElement.GetProperty("results").EnumerateArray())
                    {
                        string title = item.GetProperty("title").GetString() ?? "";
                        string url = GetTextUrl(item);

                        if (url != "")
                            popularList.Items.Add(new Book(title, url));

                        if (popularList.Items.Count == 100)
                            return;
                    }
                }
            }
            catch
            {
                booksText.Text = "Помилка завантаження списку.";
            }
        }

        async void SearchBooks(object sender, EventArgs e)
        {
            try
            {
                searchList.Items.Clear();

                string url = "https://gutendex.com/books/?search=" +
                    Uri.EscapeDataString(query.Text);

                string json = await http.GetStringAsync(url);

                using JsonDocument doc = JsonDocument.Parse(json);

                foreach (JsonElement item in
                    doc.RootElement.GetProperty("results").EnumerateArray())
                {
                    string title = item.GetProperty("title").GetString() ?? "";
                    string textUrl = GetTextUrl(item);

                    if (textUrl != "")
                        searchList.Items.Add(new Book(title, textUrl));
                }

                if (searchList.Items.Count == 0)
                    searchText.Text = "Книги не знайдено.";
            }
            catch
            {
                searchText.Text = "Помилка пошуку.";
            }
        }

        string GetTextUrl(JsonElement item)
        {
            if (item.TryGetProperty("formats", out JsonElement formats))
            {
                foreach (JsonProperty format in formats.EnumerateObject())
                {
                    if (format.Name.StartsWith("text/plain") &&
                        format.Value.ValueKind == JsonValueKind.String)
                        return format.Value.GetString() ?? "";
                }
            }

            return "";
        }

        async System.Threading.Tasks.Task<string> GetBookText(string url)
        {
            try
            {
                if (url == "") return "Текст недоступний.";

                return await http.GetStringAsync(url);
            }
            catch
            {
                return "Помилка завантаження тексту.";
            }
        }

        class Book
        {
            public string Title;
            public string Url;

            public Book(string title, string url)
            {
                Title = title;
                Url = url;
            }

            public override string ToString()
            {
                return Title;
            }
        }
    }
}

