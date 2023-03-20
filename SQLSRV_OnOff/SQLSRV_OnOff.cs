using System.ServiceProcess;
using System.Xml;

namespace SQLSRV_OnOff
{
    public partial class SQLSRV_OnOff : Form
    {
        private class Servicio
        {
            public string Nombre { get; set; }
            public string Texto { get; set; }
            public Servicio(string nombre, string texto)
            {
                Nombre = nombre;
                Texto = texto;
            }
        }
        List<Servicio> servicios;
        readonly Bitmap iconoON = Properties.Resources.bombilla_ON;
        readonly Bitmap iconoOFF = Properties.Resources.bombilla_OFF;
        private ServiceController serviceController = new ServiceController();
        private NotifyIcon trayIcon = new NotifyIcon();
        private ContextMenuStrip menu = new ContextMenuStrip();
        private ToolStripMenuItem submenu = new ToolStripMenuItem("Activar/Desactivar servicio");

        public SQLSRV_OnOff()
        {
            InitializeComponent();
            servicios = LeerConfigXML(@"Apli_config.xml");

            trayIcon.Icon = Icon.FromHandle((Properties.Resources.trayicon).GetHicon()); // Recurso (PNG) --> Icono
            trayIcon.Text = "Gestión de servicios";
            foreach (var item in servicios)
                submenu.DropDownItems.Add(item.Texto, iconoOFF, OnOffServicio);        
            menu.Items.Add(submenu);
            menu.Items.Add("-");
            menu.Items.Add("Salir", null, OnSalir);
            trayIcon.ContextMenuStrip = menu;

            UpdateTrayIcon();
            trayIcon.Visible = true;
        }

        private void SQLSRV_OnOff_Shown(object sender, EventArgs e)
        {
            this.Hide();
        }
        
        private List<Servicio> LeerConfigXML(string ficheroConfig)
        {
            XmlDocument xmlConfig = new XmlDocument ();
            xmlConfig.Load(ficheroConfig);
            XmlNodeList serviciosNodos = xmlConfig.SelectNodes("//SRV");

            List<Servicio> servicios = new List<Servicio>();
            foreach (XmlNode servicio in serviciosNodos)
                servicios.Add(new Servicio(servicio.Attributes["Nombre"].Value, servicio.Attributes["Texto"].Value));
            
            return servicios;
        }

        private void UpdateTrayIcon()
        {
            ToolStripMenuItem itemSub;
            bool algunServicioActivo = false;
            int i = 0;
            foreach (Servicio servicio in servicios)
            {
                itemSub = (ToolStripMenuItem)submenu.DropDownItems[i++];
                serviceController.ServiceName = servicio.Nombre;
                try
                {
                    if (serviceController.Status == ServiceControllerStatus.Running)
                    {
                        itemSub.Image = iconoON;
                        algunServicioActivo = true;
                    }
                    else
                    {
                        itemSub.Image = iconoOFF;
                    }
                }
                catch
                {
                    itemSub.Image = null;
                }
            }
            submenu.Image = (algunServicioActivo) ? iconoON : iconoOFF;
        }

        private void OnOffServicio(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            Servicio servicioEncontrado = servicios.Find(s => s.Texto == item.Text);
            try
            {
                serviceController.ServiceName = servicioEncontrado.Nombre;
                var estadoNuevo = serviceController.Status;
                if (serviceController.Status == ServiceControllerStatus.Running)
                {
                    estadoNuevo = ServiceControllerStatus.Stopped;
                    serviceController.Stop();
                }
                else
                {
                    estadoNuevo = ServiceControllerStatus.Running;
                    serviceController.Start();
                }
                serviceController.WaitForStatus(estadoNuevo, new TimeSpan(0, 0, 10));
                UpdateTrayIcon();
            } catch { }
        }

        private void OnSalir(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}