using System;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;

namespace NBear.Tools.EntityDesignToEntityVsPlugin
{
	/// <summary>The object for implementing an Add-in.</summary>
	/// <seealso class='IDTExtensibility2' />
	public class Connect : IDTExtensibility2
	{
		/// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
		public Connect()
		{
		}

		/// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
		/// <param term='application'>Root object of the host application.</param>
		/// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
		/// <param term='addInInst'>Object representing this Add-in.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
		{
			_applicationObject = (DTE2)application;
			_addInInstance = (AddIn)addInInst;

            _buildEvents = _applicationObject.Events.BuildEvents;
            _buildEvents.OnBuildDone += new _dispBuildEvents_OnBuildDoneEventHandler(BuildEvents_OnBuildDone);
        }

		/// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
		/// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
		{
            _buildEvents.OnBuildDone -= new _dispBuildEvents_OnBuildDoneEventHandler(BuildEvents_OnBuildDone);
		}

		/// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />		
        public void OnAddInsUpdate(ref Array custom)
        {
        }

        private void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            this._applicationObject.ToolWindows.ErrorList.ShowErrors = true;
            this._applicationObject.ToolWindows.ErrorList.ShowWarnings = false;
            this._applicationObject.ToolWindows.ErrorList.ShowMessages = false;
            if (this._applicationObject.ToolWindows.ErrorList.ErrorItems.Count == 0)
            {
                foreach (Project item in (Array)this._applicationObject.ActiveSolutionProjects)
                {
                    string designRootPath = Path.GetDirectoryName(item.FileName);
                    string configFile = designRootPath + "\\EntityDesignToEntityConfig.xml";
                    if (File.Exists(configFile))
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(Path.GetDirectoryName(this.GetType().Assembly.CodeBase).Replace("file:\\", "") + "\\NBear.Tools.EntityDesignToEntity.exe", "\"" + designRootPath + "\"");
                        }
                        catch (Exception ex)
                        {
                            System.Windows.Forms.MessageBox.Show("EntityDesignToEntity auto generating error:\r\n" + ex.ToString());
                        }
                    }
                }
            }
        }

		/// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnStartupComplete(ref Array custom)
		{
        }

		/// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnBeginShutdown(ref Array custom)
		{
		}
		
		private DTE2 _applicationObject;
		private AddIn _addInInstance;
        private EnvDTE.BuildEvents _buildEvents;
	}
}