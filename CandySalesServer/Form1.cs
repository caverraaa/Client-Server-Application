using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json; // NuGet: Newtonsoft.Json

namespace CandySalesServer
{
    public partial class Form1 : Form
    {
        
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

        
        private TcpListener listener;
        private CancellationTokenSource cts;
        private readonly object fileLock = new object();
        private BindingList<CandySale> sales = new BindingList<CandySale>();
        private string dataFile = "candy_sales.json";

        
        private DataGridView dgvSales;
        private Button btnStart, btnStop, btnLoad, btnSave, btnBrowse;
        private TextBox txtPort, txtFile;
        private ListBox lstLog;
        private Label lblPort, lblFile, lblStatus;
        private TableLayoutPanel tlp;

        public Form1()
        {
            InitializeComponent();
            InitializeUI();
            dgvSales.DataSource = sales;
            txtPort.Text = "5555";
            txtFile.Text = dataFile;

            if (File.Exists(dataFile))
            {
                try
                {
                    LoadFromFile();
                }
                catch
                {
                    
                }
            }
        }

        private void InitializeUI()
        {
            this.Text = "Сервер - Облік продажу цукерок";
            this.Width = 1000;
            this.Height = 650;

            tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            this.Controls.Add(tlp);

            // Left panel
            var leftPanel = new Panel { Dock = DockStyle.Fill };
            dgvSales = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = true,
                ReadOnly = false,
                AllowUserToAddRows = false
            };
            leftPanel.Controls.Add(dgvSales);

            var leftTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight
            };
            btnStart = new Button { Text = "Start Server", Width = 100 };
            btnStop = new Button { Text = "Stop Server", Width = 100, Enabled = false };
            btnLoad = new Button { Text = "Load File", Width = 90 };
            btnSave = new Button { Text = "Save File", Width = 90 };
            leftTop.Controls.AddRange(new Control[] { btnStart, btnStop, btnLoad, btnSave });

            leftPanel.Controls.Add(leftTop);
            tlp.Controls.Add(leftPanel, 0, 0);

          
            var rightPanel = new Panel { Dock = DockStyle.Fill };

            var rightTop = new Panel { Dock = DockStyle.Top, Height = 90 };
            lblPort = new Label
            {
                Text = "Port:",
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };
            txtPort = new TextBox
            {
                Location = new System.Drawing.Point(50, 7),
                Width = 80
            };

            lblFile = new Label
            {
                Text = "Data file:",
                Location = new System.Drawing.Point(10, 40),
                AutoSize = true
            };
            txtFile = new TextBox
            {
                Location = new System.Drawing.Point(70, 37),
                Width = 180
            };
            btnBrowse = new Button
            {
                Text = "Browse",
                Location = new System.Drawing.Point(260, 35),
                Width = 70
            };

            lblStatus = new Label
            {
                Text = "Status: Stopped",
                Location = new System.Drawing.Point(10, 70),
                AutoSize = true,
                ForeColor = System.Drawing.Color.Red
            };

            rightTop.Controls.AddRange(new Control[] { lblPort, txtPort, lblFile, txtFile, btnBrowse, lblStatus });
            rightPanel.Controls.Add(rightTop);

            var lblLog = new Label { Text = "Server Log:", Dock = DockStyle.Top, Height = 20 };
            rightPanel.Controls.Add(lblLog);

            lstLog = new ListBox { Dock = DockStyle.Fill };
            rightPanel.Controls.Add(lstLog);

            tlp.Controls.Add(rightPanel, 1, 0);

            // Events
            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            btnLoad.Click += BtnLoad_Click;
            btnSave.Click += BtnSave_Click;
            btnBrowse.Click += BtnBrowse_Click;
            this.FormClosing += ServerForm_FormClosing;
        }

        private void Log(string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => Log(text)));
                return;
            }
            lstLog.Items.Insert(0, string.Format("[{0}] {1}",
                DateTime.Now.ToString("HH:mm:ss"), text));
        }

        private void UpdateStatus(string text, bool isRunning)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatus(text, isRunning)));
                return;
            }
            lblStatus.Text = "Status: " + text;
            lblStatus.ForeColor = isRunning ? System.Drawing.Color.Green : System.Drawing.Color.Red;
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "JSON files|*.json|All files|*.*";
                dlg.FileName = txtFile.Text;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtFile.Text = dlg.FileName;
                    dataFile = dlg.FileName;
                }
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            dataFile = txtFile.Text;
            try
            {
                LoadFromFile();
                Log("Loaded file: " + dataFile);
                MessageBox.Show("File loaded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log("Load error: " + ex.Message);
                MessageBox.Show("Load error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            dataFile = txtFile.Text;
            try
            {
                SaveToFile();
                Log("Saved to file: " + dataFile);
                MessageBox.Show("File saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log("Save error: " + ex.Message);
                MessageBox.Show("Save error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            int port;
            if (!int.TryParse(txtPort.Text, out port))
            {
                MessageBox.Show("Invalid port number!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            txtPort.Enabled = false;

            dataFile = txtFile.Text;
            cts = new CancellationTokenSource();
            Task.Run(() => RunListenerAsync(port, cts.Token));
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopServer();
        }

        private void ServerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer();
        }

        private void StopServer()
        {
            try
            {
                if (cts != null)
                {
                    cts.Cancel();
                    if (listener != null)
                    {
                        try { listener.Stop(); } catch { }
                    }
                    cts = null;
                    Log("Server stopped.");
                    UpdateStatus("Stopped", false);
                }
            }
            catch (Exception ex)
            {
                Log("Stop error: " + ex.Message);
            }
            finally
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        btnStart.Enabled = true;
                        btnStop.Enabled = false;
                        txtPort.Enabled = true;
                    }));
                }
                else
                {
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                    txtPort.Enabled = true;
                }
            }
        }

        private async Task RunListenerAsync(int port, CancellationToken token)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                Log("Server started on port " + port);
                UpdateStatus("Running on port " + port, true);

                lock (fileLock)
                {
                    if (!File.Exists(dataFile))
                        File.WriteAllText(dataFile, "[]");
                    var initial = ReadSalesFromFileInternal();
                    UpdateSalesBinding(initial);
                }

                while (!token.IsCancellationRequested)
                {
                    TcpClient client = null;
                    try
                    {
                        client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log("Accept error: " + ex.Message);
                        continue;
                    }

                    Log("Client connected: " + client.Client.RemoteEndPoint);
                    var ignore = Task.Run(() => HandleClientAsync(client, token));
                }
            }
            catch (Exception ex)
            {
                Log("Listener error: " + ex.Message);
                UpdateStatus("Error: " + ex.Message, false);
            }
            finally
            {
                try { listener.Stop(); } catch { }
                Log("Listener loop ended.");
                UpdateStatus("Stopped", false);

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        btnStart.Enabled = true;
                        btnStop.Enabled = false;
                        txtPort.Enabled = true;
                    }));
                }
                else
                {
                    btnStart.Enabled = true;
                    btnStop.Enabled = false;
                    txtPort.Enabled = true;
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            try
            {
                using (client)
                using (var ns = client.GetStream())
                using (var sr = new StreamReader(ns, Encoding.UTF8))
                using (var sw = new StreamWriter(ns, Encoding.UTF8) { AutoFlush = true })
                {
                    string line;
                    while (!token.IsCancellationRequested &&
                           (line = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        Log(string.Format("Received: {0}", line.Substring(0, Math.Min(50, line.Length))));
                        string response = ProcessCommand(line);
                        await sw.WriteLineAsync(response).ConfigureAwait(false);
                        Log("Response sent");
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Client handling error: " + ex.Message);
            }
            finally
            {
                try { Log("Client disconnected"); } catch { }
            }
        }

        private string ProcessCommand(string cmdline)
        {
            try
            {
                var parts = cmdline.Split(new[] { '|' }, 2);
                var cmd = parts[0].Trim().ToLower();
                var payload = parts.Length > 1 ? parts[1] : "";

                switch (cmd)
                {
                    case "view":
                        return MakeOkResponse(GetAllSalesSnapshot());
                    case "add":
                        return AddSale(payload);
                    case "delete":
                        return DeleteSale(payload);
                    case "edit":
                        return EditSale(payload);
                    case "search":
                        return SearchSales(payload);
                    default:
                        return MakeErrorResponse("Unknown command");
                }
            }
            catch (Exception ex)
            {
                return MakeErrorResponse("Server error: " + ex.Message);
            }
        }

        private List<CandySale> GetAllSalesSnapshot()
        {
            lock (fileLock)
            {
                return sales.ToList();
            }
        }

        private string AddSale(string saleJson)
        {
            try
            {
                var newSale = JsonConvert.DeserializeObject<CandySale>(saleJson);
                if (newSale == null) return MakeErrorResponse("Invalid sale data");

                lock (fileLock)
                {
                    int newId = 1;
                    if (sales.Count > 0) newId = sales.Max(s => s.Id) + 1;
                    newSale.Id = newId;
                    newSale.TotalAmount = newSale.Quantity * newSale.PricePerKg;

                    
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => sales.Add(newSale)));
                    }
                    else
                    {
                        sales.Add(newSale);
                    }

                    SaveToFileInternal();
                }

                Log($"Added sale: {newSale.CandyName}");
                return MakeOkResponse(newSale);
            }
            catch (Exception ex)
            {
                return MakeErrorResponse("Add error: " + ex.Message);
            }
        }

        private string DeleteSale(string idStr)
        {
            int id;
            if (!int.TryParse(idStr, out id))
                return MakeErrorResponse("Invalid id");

            lock (fileLock)
            {
                var toRemove = sales.FirstOrDefault(s => s.Id == id);
                if (toRemove == null) return MakeErrorResponse("Sale not found");

                
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => sales.Remove(toRemove)));
                }
                else
                {
                    sales.Remove(toRemove);
                }

                SaveToFileInternal();
            }

            Log($"Deleted sale ID: {id}");
            return MakeOkResponse(new { deleted = id });
        }

        private string EditSale(string saleJson)
        {
            try
            {
                var edited = JsonConvert.DeserializeObject<CandySale>(saleJson);
                if (edited == null) return MakeErrorResponse("Invalid sale data");

                lock (fileLock)
                {
                    var existing = sales.FirstOrDefault(s => s.Id == edited.Id);
                    if (existing == null) return MakeErrorResponse("Sale not found");

                    
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            existing.CandyName = edited.CandyName;
                            existing.CandyType = edited.CandyType;
                            existing.Quantity = edited.Quantity;
                            existing.PricePerKg = edited.PricePerKg;
                            existing.CustomerName = edited.CustomerName;
                            existing.SaleDate = edited.SaleDate;
                            existing.TotalAmount = existing.Quantity * existing.PricePerKg;
                        }));
                    }
                    else
                    {
                        existing.CandyName = edited.CandyName;
                        existing.CandyType = edited.CandyType;
                        existing.Quantity = edited.Quantity;
                        existing.PricePerKg = edited.PricePerKg;
                        existing.CustomerName = edited.CustomerName;
                        existing.SaleDate = edited.SaleDate;
                        existing.TotalAmount = existing.Quantity * existing.PricePerKg;
                    }

                    SaveToFileInternal();
                }

                Log($"Edited sale ID: {edited.Id}");
                return MakeOkResponse(edited);
            }
            catch (Exception ex)
            {
                return MakeErrorResponse("Edit error: " + ex.Message);
            }
        }

        private string SearchSales(string query)
        {
            query = (query ?? "").Trim().ToLower();
            List<CandySale> found;

            lock (fileLock)
            {
                if (string.IsNullOrEmpty(query))
                {
                    found = sales.ToList();
                }
                else
                {
                    found = sales.Where(s =>
                        (s.CandyName ?? "").ToLower().Contains(query) ||
                        (s.CandyType ?? "").ToLower().Contains(query) ||
                        (s.CustomerName ?? "").ToLower().Contains(query) ||
                        s.Id.ToString() == query).ToList();
                }
            }

            Log($"Search query: {query}, found: {found.Count}");
            return MakeOkResponse(found);
        }

        private void LoadFromFile()
        {
            lock (fileLock)
            {
                var list = ReadSalesFromFileInternal();
                UpdateSalesBinding(list);
            }
        }

        private List<CandySale> ReadSalesFromFileInternal()
        {
            if (!File.Exists(dataFile)) return new List<CandySale>();
            var json = File.ReadAllText(dataFile);
            var list = JsonConvert.DeserializeObject<List<CandySale>>(json) ?? new List<CandySale>();
            return list;
        }

        private void SaveToFile()
        {
            lock (fileLock)
            {
                SaveToFileInternal();
            }
        }

        private void SaveToFileInternal()
        {
            var arr = sales.ToList();
            var json = JsonConvert.SerializeObject(arr, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(dataFile, json);
        }

        private void UpdateSalesBinding(List<CandySale> list)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateSalesBinding(list)));
                return;
            }
            sales = new BindingList<CandySale>(list);
            dgvSales.DataSource = sales;
        }

        private string MakeOkResponse(object data)
        {
            var wrapper = new { status = "ok", data = data };
            return JsonConvert.SerializeObject(wrapper);
        }

        private string MakeErrorResponse(string message)
        {
            var wrapper = new { status = "error", message = message };
            return JsonConvert.SerializeObject(wrapper);
        }
    }
}