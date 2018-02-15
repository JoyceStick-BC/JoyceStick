#if UNITY_EDITOR
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2014 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System;



public class AkWwiseXMLWatcher
{
	private System.IO.FileSystemWatcher XmlWatcher;
	private string SoundBankFolder;
	
	private static AkWwiseXMLWatcher Instance = null;
	
	public static AkWwiseXMLWatcher GetInstance()
	{
		if (Instance == null)
		{
			Instance = new AkWwiseXMLWatcher ();
		}
		
		return Instance;
	}
	
	
	private AkWwiseXMLWatcher()
	{
		XmlWatcher 			= new System.IO.FileSystemWatcher ();
		SoundBankFolder 	= AkBasePathGetter.GetSoundbankBasePath();
		
		try
		{
			XmlWatcher.Path = SoundBankFolder;
			XmlWatcher.NotifyFilter = System.IO.NotifyFilters.LastWrite; 
			
			// Event handlers that are watching for specific event
			XmlWatcher.Created += new System.IO.FileSystemEventHandler(RaisePopulateFlag);
			XmlWatcher.Changed += new System.IO.FileSystemEventHandler(RaisePopulateFlag);
			
			XmlWatcher.Filter = "*.xml";
			XmlWatcher.IncludeSubdirectories = true;
		}
		catch( Exception )
		{
			// Deliberately left empty
		}
	}
	
	public void StartXMLWatcher()
	{
		XmlWatcher.EnableRaisingEvents = true; 
	}
	
	public void StopXMLWatcher()
	{
		XmlWatcher.EnableRaisingEvents = false;
	}

	
	void RaisePopulateFlag(object sender, System.IO.FileSystemEventArgs e)
	{	
		// Signal the main thread it's time to populate (cannot run populate somewhere else than on main thread)
		AkAmbientInspector.populateSoundBank = true;
	}
}
#endif