namespace TrackingCodeProcessor
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ChateauSiteflowOrderProcessorServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.ChateauSiteflowOrderProcessorServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // ChateauSiteflowOrderProcessorServiceProcessInstaller
            // 
            this.ChateauSiteflowOrderProcessorServiceProcessInstaller.Password = null;
            this.ChateauSiteflowOrderProcessorServiceProcessInstaller.Username = null;
            // 
            // ChateauSiteflowOrderProcessorServiceInstaller
            // 
            this.ChateauSiteflowOrderProcessorServiceInstaller.ServiceName = "ChateauSiteflowOrderProcessorService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ChateauSiteflowOrderProcessorServiceProcessInstaller,
            this.ChateauSiteflowOrderProcessorServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller ChateauSiteflowOrderProcessorServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller ChateauSiteflowOrderProcessorServiceInstaller;
    }
}