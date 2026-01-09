using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace rpg_deneme.UI;

public class QuantityDialog : Form
{
	private IContainer components = null;

	private NumericUpDown nmAmount;

	private Button btnOK;

	private Button btnCancel;

	public int SelectedQuantity { get; private set; }

	public QuantityDialog(int maxAmount)
	{
		InitializeComponent();
		nmAmount.Maximum = maxAmount;
		nmAmount.Minimum = 1m;
		nmAmount.Value = maxAmount;
	}

	private void btnOK_Click(object sender, EventArgs e)
	{
		SelectedQuantity = (int)nmAmount.Value;
		base.DialogResult = DialogResult.OK;
		Close();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.nmAmount = new System.Windows.Forms.NumericUpDown();
		this.btnOK = new System.Windows.Forms.Button();
		this.btnCancel = new System.Windows.Forms.Button();
		((System.ComponentModel.ISupportInitialize)this.nmAmount).BeginInit();
		base.SuspendLayout();
		this.nmAmount.Location = new System.Drawing.Point(60, 45);
		this.nmAmount.Name = "nmAmount";
		this.nmAmount.Size = new System.Drawing.Size(120, 23);
		this.nmAmount.TabIndex = 0;
		this.btnOK.Location = new System.Drawing.Point(41, 94);
		this.btnOK.Name = "btnOK";
		this.btnOK.Size = new System.Drawing.Size(75, 23);
		this.btnOK.TabIndex = 1;
		this.btnOK.Text = "OK";
		this.btnOK.UseVisualStyleBackColor = true;
		this.btnOK.Click += new System.EventHandler(btnOK_Click);
		this.btnCancel.Location = new System.Drawing.Point(122, 94);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(75, 23);
		this.btnCancel.TabIndex = 2;
		this.btnCancel.Text = "button2";
		this.btnCancel.UseVisualStyleBackColor = true;
		base.AutoScaleDimensions = new System.Drawing.SizeF(7f, 15f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(242, 160);
		base.Controls.Add(this.btnCancel);
		base.Controls.Add(this.btnOK);
		base.Controls.Add(this.nmAmount);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "QuantityDialog";
		base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "QuantityDialog";
		((System.ComponentModel.ISupportInitialize)this.nmAmount).EndInit();
		base.ResumeLayout(false);
	}
}
