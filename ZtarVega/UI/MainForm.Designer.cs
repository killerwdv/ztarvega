namespace ZtarVega.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MenuStrip _menuStrip;
        private System.Windows.Forms.ToolStripMenuItem _gameMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _toggleFavoriteMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _toolsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _scanRomsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _settingsMenuItem;
        private System.Windows.Forms.TableLayoutPanel _mainLayout;
        private System.Windows.Forms.ToolStripMenuItem _helpMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _aboutMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _addGameMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _editGameMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _deleteGameMenuItem;
        private System.Windows.Forms.ToolStripSeparator _gameMenuSeparator;
        private System.Windows.Forms.ToolStripMenuItem _importDatMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _setImagesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _importMetadataMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _importImagesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _downloadImagesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem _guideMenuItem;
        private GameListPanel _gameListPanel;
        private GameDetailsPanel _detailsPanel;
        private GameArtworkPanel _artworkPanel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._menuStrip = new System.Windows.Forms.MenuStrip();
            this._gameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toggleFavoriteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._toolsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._scanRomsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._settingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this._gameListPanel = new ZtarVega.UI.GameListPanel();
            this._detailsPanel = new ZtarVega.UI.GameDetailsPanel();
            this._artworkPanel = new ZtarVega.UI.GameArtworkPanel();
            this._helpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._addGameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._editGameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._deleteGameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._gameMenuSeparator = new System.Windows.Forms.ToolStripSeparator();
            this._importDatMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._setImagesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._importMetadataMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._importImagesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._downloadImagesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._guideMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this._menuStrip.SuspendLayout();
            this._mainLayout.SuspendLayout();
            this.SuspendLayout();
            //
            // _menuStrip
            //
            this._menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._gameMenuItem,
            this._toolsMenuItem,
            this._helpMenuItem});
            this._menuStrip.Name = "_menuStrip";
            //
            // _gameMenuItem
            //
            this._gameMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._addGameMenuItem,
            this._editGameMenuItem,
            this._setImagesMenuItem,
            this._deleteGameMenuItem,
            this._gameMenuSeparator,
            this._toggleFavoriteMenuItem});
            this._gameMenuItem.Name = "_gameMenuItem";
            this._gameMenuItem.Text = "&Game";
            //
            // _addGameMenuItem
            //
            this._addGameMenuItem.Name = "_addGameMenuItem";
            this._addGameMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this._addGameMenuItem.Text = "&Add Game...";
            //
            // _editGameMenuItem
            //
            this._editGameMenuItem.Name = "_editGameMenuItem";
            this._editGameMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this._editGameMenuItem.Text = "&Edit Game...";
            //
            // _setImagesMenuItem
            //
            this._setImagesMenuItem.Name = "_setImagesMenuItem";
            this._setImagesMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
            this._setImagesMenuItem.Text = "Set &Images...";
            //
            // _deleteGameMenuItem
            //
            this._deleteGameMenuItem.Name = "_deleteGameMenuItem";
            this._deleteGameMenuItem.Text = "&Delete Game...";
            //
            // _gameMenuSeparator
            //
            this._gameMenuSeparator.Name = "_gameMenuSeparator";
            //
            // _toggleFavoriteMenuItem
            //
            this._toggleFavoriteMenuItem.Name = "_toggleFavoriteMenuItem";
            this._toggleFavoriteMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
            this._toggleFavoriteMenuItem.Text = "Toggle &Favorite";
            //
            // _toolsMenuItem
            //
            this._toolsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._importDatMenuItem,
            this._importMetadataMenuItem,
            this._importImagesMenuItem,
            this._downloadImagesMenuItem,
            this._scanRomsMenuItem,
            this._settingsMenuItem});
            this._toolsMenuItem.Name = "_toolsMenuItem";
            this._toolsMenuItem.Text = "&Tools";
            //
            // _scanRomsMenuItem
            //
            this._scanRomsMenuItem.Name = "_scanRomsMenuItem";
            this._scanRomsMenuItem.Text = "Scan &ROMs...";
            //
            // _importDatMenuItem
            //
            this._importDatMenuItem.Name = "_importDatMenuItem";
            this._importDatMenuItem.Text = "&Import Catalog from DAT...";
            //
            // _importMetadataMenuItem
            //
            this._importMetadataMenuItem.Name = "_importMetadataMenuItem";
            this._importMetadataMenuItem.Text = "Import &Metadata...";
            //
            // _importImagesMenuItem
            //
            this._importImagesMenuItem.Name = "_importImagesMenuItem";
            this._importImagesMenuItem.Text = "Import I&mages from Folder...";
            //
            // _downloadImagesMenuItem
            //
            this._downloadImagesMenuItem.Name = "_downloadImagesMenuItem";
            this._downloadImagesMenuItem.Text = "Do&wnload Images...";
            //
            // _settingsMenuItem
            //
            this._settingsMenuItem.Name = "_settingsMenuItem";
            this._settingsMenuItem.Text = "&Settings...";
            //
            // _helpMenuItem
            //
            this._helpMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._guideMenuItem,
            this._aboutMenuItem});
            this._helpMenuItem.Name = "_helpMenuItem";
            this._helpMenuItem.Text = "&Help";
            //
            // _guideMenuItem
            //
            this._guideMenuItem.Name = "_guideMenuItem";
            this._guideMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this._guideMenuItem.Text = "&Getting started...";
            //
            // _aboutMenuItem
            //
            this._aboutMenuItem.Name = "_aboutMenuItem";
            this._aboutMenuItem.Text = "&About ZtarVega";
            //
            // _mainLayout
            //
            this._mainLayout.ColumnCount = 3;
            this._mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 28F));
            this._mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 44F));
            this._mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 28F));
            this._mainLayout.Controls.Add(this._gameListPanel, 0, 0);
            this._mainLayout.Controls.Add(this._detailsPanel, 1, 0);
            this._mainLayout.Controls.Add(this._artworkPanel, 2, 0);
            this._mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this._mainLayout.Name = "_mainLayout";
            this._mainLayout.RowCount = 1;
            this._mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            //
            // _gameListPanel
            //
            this._gameListPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gameListPanel.Name = "_gameListPanel";
            //
            // _detailsPanel
            //
            this._detailsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._detailsPanel.Name = "_detailsPanel";
            //
            // _artworkPanel
            //
            this._artworkPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._artworkPanel.Name = "_artworkPanel";
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 650);
            this.Controls.Add(this._mainLayout);
            this.Controls.Add(this._menuStrip);
            this.MainMenuStrip = this._menuStrip;
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ZtarVega";
            this._menuStrip.ResumeLayout(false);
            this._menuStrip.PerformLayout();
            this._mainLayout.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}