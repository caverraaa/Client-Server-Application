using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace CandySalesClient
{
    public partial class Form1 : Form
    {
        // Model
        public class CandySale
        {
            public int Id { get; set; }
            public string CandyName { get; set; } = "";
            public string CandyType { get; set; } = "";
            public int Quantity { get; set; }
            public decimal PricePerKg { get; set; }
            public string CustomerName { get; set; } = "";
            public DateTime SaleDate { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public class ServerResponse<T>
        {
            public string status { get; set; }
            public T data { get; set; }
            public string message { get; set; }
        }

        // Server settings
        private string serverHost = "127.0.0.1";
        private int serverPort = 5555;

        // UI controls
        private DataGridView dgvSales;
        private Button btnView, btnAdd, btnEdit, btnDelete, btnSearch;
        private TextBox txtSearch;
        private Label lblStatus, lblTotal;

        public Form1()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Клієнт - Облік продажу цукерок";
            this.Width = 1000;
            this.Height = 600;

            var mainTlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            mainTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            this.Controls.Add(mainTlp);

            // Top controls (buttons + search)
            var topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5)
            };

            btnView = new Button { Text = "Переглянути всі", Width = 120, Height = 30 };
            btnAdd = new Button { Text = "Додати", Width = 100, Height = 30 };
            btnEdit = new Button { Text = "Редагувати", Width = 110, Height = 30 };
            btnDelete = new Button { Text = "Видалити", Width = 100, Height = 30 };

            var lblSearch = new Label
            {
                Text = "Пошук:",
                AutoSize = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Padding = new Padding(10, 8, 5, 0)
            };
            txtSearch = new TextBox { Width = 200, Height = 30 };
            btnSearch = new Button { Text = "Знайти", Width = 80, Height = 30 };

            topPanel.Controls.AddRange(new Control[]
            {
                btnView, btnAdd, btnEdit, btnDelete, lblSearch, txtSearch, btnSearch
            });
            mainTlp.Controls.Add(topPanel, 0, 0);

            // DataGridView
            dgvSales = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false
            };
            mainTlp.Controls.Add(dgvSales, 0, 1);

            // Bottom panel
            var bottomPanel = new Panel { Dock = DockStyle.Fill, Height = 50 };

            lblStatus = new Label
            {
                Text = "Готово",
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };

            lblTotal = new Label
            {
                Text = "Загальна виручка: 0.00 грн",
                Location = new System.Drawing.Point(10, 30),
                AutoSize = true,
                Font = new System.Drawing.Font(this.Font.FontFamily, 9, System.Drawing.FontStyle.Bold)
            };

            bottomPanel.Controls.Add(lblStatus);
            bottomPanel.Controls.Add(lblTotal);
            mainTlp.Controls.Add(bottomPanel, 0, 2);

            // Events
            btnView.Click += BtnView_Click;
            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            btnSearch.Click += BtnSearch_Click;
            txtSearch.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)13)
                {
                    BtnSearch_Click(s, e);
                    e.Handled = true;
                }
            };

            // Load all on start
            this.Load += (s, e) => LoadAll();
        }

        private void SetStatus(string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(SetStatus), text);
                return;
            }
            lblStatus.Text = text;
        }

        private void UpdateTotal(List<CandySale> salesList)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<List<CandySale>>(UpdateTotal), salesList);
                return;
            }

            if (salesList != null && salesList.Count > 0)
            {
                decimal total = 0;
                foreach (var sale in salesList)
                {
                    total += sale.TotalAmount;
                }
                lblTotal.Text = $"Загальна виручка: {total:N2} грн | Записів: {salesList.Count}";
            }
            else
            {
                lblTotal.Text = "Загальна виручка: 0.00 грн | Записів: 0";
            }
        }

        private void BtnView_Click(object sender, EventArgs e)
        {
            LoadAll();
        }

        private void LoadAll()
        {
            SetStatus("Завантаження...");
            try
            {
                var responseJson = SendCommand("view|");
                var resp = JsonConvert.DeserializeObject<ServerResponse<List<CandySale>>>(responseJson);

                if (resp != null && resp.status == "ok")
                {
                    dgvSales.DataSource = resp.data;
                    UpdateTotal(resp.data);
                    SetStatus($"Завантажено записів: {(resp.data != null ? resp.data.Count : 0)}");
                }
                else
                {
                    MessageBox.Show("Помилка: " + (resp != null ? resp.message : "Невірна відповідь"),
                        "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetStatus("Помилка завантаження");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження: " + ex.Message,
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Помилка");
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var f = new SaleForm())
            {
                if (f.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var json = JsonConvert.SerializeObject(f.Sale);
                        var responseJson = SendCommand("add|" + json);
                        var resp = JsonConvert.DeserializeObject<ServerResponse<CandySale>>(responseJson);

                        if (resp != null && resp.status == "ok")
                        {
                            LoadAll();
                            MessageBox.Show("Продаж успішно додано!", "Успіх",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Помилка додавання: " + (resp != null ? resp.message : "Невірна відповідь"),
                                "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Помилка додавання: " + ex.Message,
                            "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private CandySale GetSelectedSale()
        {
            if (dgvSales.CurrentRow == null) return null;
            return dgvSales.CurrentRow.DataBoundItem as CandySale;
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedSale();
            if (selected == null)
            {
                MessageBox.Show("Спочатку виберіть запис для редагування!", "Увага",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var copy = new CandySale
            {
                Id = selected.Id,
                CandyName = selected.CandyName,
                CandyType = selected.CandyType,
                Quantity = selected.Quantity,
                PricePerKg = selected.PricePerKg,
                CustomerName = selected.CustomerName,
                SaleDate = selected.SaleDate,
                TotalAmount = selected.TotalAmount
            };

            using (var f = new SaleForm(copy))
            {
                if (f.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var json = JsonConvert.SerializeObject(f.Sale);
                        var responseJson = SendCommand("edit|" + json);
                        var resp = JsonConvert.DeserializeObject<ServerResponse<CandySale>>(responseJson);

                        if (resp != null && resp.status == "ok")
                        {
                            LoadAll();
                            MessageBox.Show("Запис успішно оновлено!", "Успіх",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Помилка редагування: " + (resp != null ? resp.message : "Невірна відповідь"),
                                "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Помилка редагування: " + ex.Message,
                            "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var selected = GetSelectedSale();
            if (selected == null)
            {
                MessageBox.Show("Спочатку виберіть запис для видалення!", "Увага",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Видалити продаж: {selected.CandyName} ({selected.CustomerName})?\nID={selected.Id}",
                "Підтвердження",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                var responseJson = SendCommand("delete|" + selected.Id);
                var resp = JsonConvert.DeserializeObject<ServerResponse<dynamic>>(responseJson);

                if (resp != null && resp.status == "ok")
                {
                    LoadAll();
                    MessageBox.Show("Запис успішно видалено!", "Успіх",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Помилка видалення: " + (resp != null ? resp.message : "Невірна відповідь"),
                        "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка видалення: " + ex.Message,
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            var query = txtSearch.Text.Trim();
            SetStatus("Пошук...");

            try
            {
                var responseJson = SendCommand("search|" + query);
                var resp = JsonConvert.DeserializeObject<ServerResponse<List<CandySale>>>(responseJson);

                if (resp != null && resp.status == "ok")
                {
                    dgvSales.DataSource = resp.data;
                    UpdateTotal(resp.data);
                    SetStatus($"Знайдено записів: {(resp.data != null ? resp.data.Count : 0)}");
                }
                else
                {
                    MessageBox.Show("Помилка пошуку: " + (resp != null ? resp.message : "Невірна відповідь"),
                        "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetStatus("Помилка пошуку");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка пошуку: " + ex.Message,
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Помилка");
            }
        }

        private string SendCommand(string command)
        {
            try
            {
                using (var tcp = new TcpClient())
                {
                    tcp.Connect(serverHost, serverPort);
                    using (var ns = tcp.GetStream())
                    using (var sw = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true })
                    using (var sr = new StreamReader(ns, Encoding.UTF8))
                    {
                        sw.WriteLine(command);
                        var response = sr.ReadLine();
                        if (response == null) throw new Exception("Немає відповіді від сервера");
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка з'єднання: " + ex.Message + "\n\nПереконайтесь, що сервер запущено!",
                    "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                var err = new { status = "error", message = ex.Message };
                return JsonConvert.SerializeObject(err);
            }
        }
    }

    // Dialog form for adding/editing sales
    public class SaleForm : Form
    {
        public Form1.CandySale Sale { get; private set; }

        private TextBox txtCandyName, txtCandyType, txtQuantity, txtPrice, txtCustomer;
        private DateTimePicker dtpSaleDate;
        private Button btnOk, btnCancel;

        public SaleForm(Form1.CandySale existingSale = null)
        {
            this.Text = existingSale == null ? "Додати продаж" : "Редагувати продаж";
            this.Width = 450;
            this.Height = 350;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            Sale = existingSale ?? new Form1.CandySale { SaleDate = DateTime.Now };

            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                Padding = new Padding(10)
            };

            for (int i = 0; i < 7; i++)
                tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Candy Name
            tlp.Controls.Add(new Label { Text = "Назва цукерок:", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, 0, 0);
            txtCandyName = new TextBox { Text = Sale.CandyName, Dock = DockStyle.Fill };
            tlp.Controls.Add(txtCandyName, 1, 0);

            // Candy Type
            tlp.Controls.Add(new Label { Text = "Тип:", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, 0, 1);
            txtCandyType = new TextBox { Text = Sale.CandyType, Dock = DockStyle.Fill };
            tlp.Controls.Add(txtCandyType, 1, 1);

            // Quantity
            tlp.Controls.Add(new Label { Text = "Кількість (кг):", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, 0, 2);
            txtQuantity = new TextBox { Text = Sale.Quantity.ToString(), Dock = DockStyle.Fill };
            tlp.Controls.Add(txtQuantity, 1, 2);

            // Price
            tlp.Controls.Add(new Label { Text = "Ціна за кг (грн):", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, 0, 3);
            txtPrice = new TextBox { Text = Sale.PricePerKg.ToString(), Dock = DockStyle.Fill };
            tlp.Controls.Add(txtPrice, 1, 3);

            // Customer
            tlp.Controls.Add(new Label { Text = "Покупець:", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, 0, 4);
            txtCustomer = new TextBox { Text = Sale.CustomerName, Dock = DockStyle.Fill };
            tlp.Controls.Add(txtCustomer, 1, 4);

            // Sale Date
            tlp.Controls.Add(new Label { Text = "Дата продажу:", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, 0, 5);
            dtpSaleDate = new DateTimePicker { Value = Sale.SaleDate, Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
            tlp.Controls.Add(dtpSaleDate, 1, 5);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 0, 0)
            };
            btnOk = new Button { Text = "OK", Width = 80, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Скасувати", Width = 80, DialogResult = DialogResult.Cancel };
            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnOk);

            tlp.Controls.Add(buttonPanel, 1, 6);

            this.Controls.Add(tlp);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            btnOk.Click += BtnOk_Click;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCandyName.Text))
            {
                MessageBox.Show("Введіть назву цукерок!", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введіть коректну кількість!", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (!decimal.TryParse(txtPrice.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Введіть коректну ціну!", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            Sale.CandyName = txtCandyName.Text.Trim();
            Sale.CandyType = txtCandyType.Text.Trim();
            Sale.Quantity = quantity;
            Sale.PricePerKg = price;
            Sale.CustomerName = txtCustomer.Text.Trim();
            Sale.SaleDate = dtpSaleDate.Value;
            Sale.TotalAmount = quantity * price;
        }
    }
}