using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace LlmAggregator
{
    public partial class EndpointConfigDialog : Form
    {
        public EndpointConfigDialog()
        {
            InitializeComponent();
        }

        public EndpointConfig[] Configs { get; set; } = new EndpointConfig[0];

        private void EndpointConfigDialog_Load(object sender, EventArgs e)
        {
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = "Name" });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "URL", DataPropertyName = "Url", Width = 400 });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "API Key / JSON Path", DataPropertyName = "ApiKey", Width = 300 });
            dataGridView1.DataSource = new BindingList<EndpointConfig>(new System.Collections.Generic.List<EndpointConfig>(Configs));

            dataGridView1.CellValidating += DataGridView1_CellValidating;
        }

        private void DataGridView1_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            // Validate URL column (index 1)
            if (e.ColumnIndex == 1)
            {
                var value = (e.FormattedValue ?? string.Empty).ToString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    dataGridView1.Rows[e.RowIndex].ErrorText = "URL is required.";
                    e.Cancel = true;
                    return;
                }
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    dataGridView1.Rows[e.RowIndex].ErrorText = "URL must be absolute and use http or https.";
                    e.Cancel = true;
                    return;
                }
                dataGridView1.Rows[e.RowIndex].ErrorText = string.Empty;
            }

            // Validate ApiKey column (index 2) if it looks like a file path
            if (e.ColumnIndex == 2)
            {
                var value = (e.FormattedValue ?? string.Empty).ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var trimmed = value.Trim();
                    if (trimmed.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("{"))
                    {
                        // If path, ensure file exists; if JSON content starting with '{' we accept
                        if (!trimmed.StartsWith("{") && !File.Exists(trimmed))
                        {
                            dataGridView1.Rows[e.RowIndex].ErrorText = "API key file not found.";
                            e.Cancel = true;
                            return;
                        }
                    }
                }
                dataGridView1.Rows[e.RowIndex].ErrorText = string.Empty;
            }
        }

        private bool ValidateAllRows()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                var urlCell = row.Cells[1].Value?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(urlCell))
                {
                    MessageBox.Show("Each endpoint must have a valid URL.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                if (!Uri.TryCreate(urlCell, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    MessageBox.Show($"Invalid URL: {urlCell}", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                var apiKeyCell = row.Cells[2].Value?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(apiKeyCell))
                {
                    var t = apiKeyCell.Trim();
                    if (t.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && !File.Exists(t))
                    {
                        var res = MessageBox.Show($"API key file not found: {t}. Continue?", "Validation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (res == DialogResult.No) return false;
                    }
                }
            }
            return true;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (!ValidateAllRows()) return;
            var list = (BindingList<EndpointConfig>)dataGridView1.DataSource;
            Configs = new EndpointConfig[list.Count];
            list.CopyTo(Configs, 0);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
