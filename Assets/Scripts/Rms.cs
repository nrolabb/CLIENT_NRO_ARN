using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Game1
{
    
    public class Rms
    {
    	public static int status;
    
    	public static sbyte[] data;
    
    	public static string filename;

        private static string cachedPath = null;
        private static readonly object rmsLock = new object();

        private static bool IsMainThread()
        {
            return Main.mainThreadId != 0 && Thread.CurrentThread.ManagedThreadId == Main.mainThreadId;
        }

    	public static void saveRMS(string filename, sbyte[] data)
    	{
    		if (data == null)
    		{
    			return;
    		}
    		lock (rmsLock)
    		{
    			try
    			{
    				__saveRMS(filename, data);
    			}
    			catch (Exception ex)
    			{
    				Debug.LogError("Error saveRMS " + filename + ": " + ex.Message);
    			}
    		}
    	}

    	public static sbyte[] loadRMS(string filename)
    	{
    		lock (rmsLock)
    		{
    			try
    			{
    				return __loadRMS(filename);
    			}
    			catch (Exception ex)
    			{
    				Debug.LogError("Error loadRMS " + filename + ": " + ex.Message);
    				return null;
    			}
    		}
    	}

    	public static string loadRMSString(string fileName)
    	{
    		sbyte[] array = loadRMS(fileName);
    		if (array == null)
    		{
    			return null;
    		}
    		DataInputStream dataInputStream = new DataInputStream(array);
    		try
    		{
    			string result = dataInputStream.readUTF();
    			dataInputStream.close();
    			return result;
    		}
    		catch (Exception ex)
    		{
    			Cout.println(ex.StackTrace);
    		}
    		return null;
    	}

    	public static void saveRMSString(string filename, string data)
    	{
    		DataOutputStream dataOutputStream = new DataOutputStream();
    		try
    		{
    			dataOutputStream.writeUTF(data);
    			saveRMS(filename, dataOutputStream.toByteArray());
    			dataOutputStream.close();
    		}
    		catch (Exception ex)
    		{
    			Cout.println(ex.StackTrace);
    		}
    	}

    	private static void _saveRMS(string filename, sbyte[] data)
    	{
    		saveRMS(filename, data);
    	}

    	private static sbyte[] _loadRMS(string filename)
    	{
    		return loadRMS(filename);
    	}

    	public static void update()
    	{
    	}
    
    	public static int loadRMSInt(string file)
    	{
    		sbyte[] array = loadRMS(file);
    		if (array == null)
    		{
    			return -1;
    		}
    		return array[0];
    	}
    
    	public static void saveRMSInt(string file, int x)
    	{
    		try
    		{
    			saveRMS(file, new sbyte[1] { (sbyte)x });
    		}
    		catch (Exception)
    		{
    		}
    	}
    
    	public static string GetiPhoneDocumentsPath()
    	{
    		if (string.IsNullOrEmpty(cachedPath))
    		{
    			try
    			{
    				cachedPath = Application.persistentDataPath;
    			}
    			catch (Exception)
    			{
    				cachedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RMS");
    				if (!Directory.Exists(cachedPath))
    				{
    					Directory.CreateDirectory(cachedPath);
    				}
    			}
    		}
    		return cachedPath;
    	}
    
    	private static void __saveRMS(string filename, sbyte[] data)
    	{
    		if (data == null)
    		{
    			return;
    		}
    		string text = GetiPhoneDocumentsPath() + "/" + filename;
    		File.WriteAllBytes(text, ArrayCast.cast(data));
    	}
    
    	private static sbyte[] __loadRMS(string filename)
    	{
    		try
    		{
    			string text = GetiPhoneDocumentsPath() + "/" + filename;
    			if (!File.Exists(text))
    			{
    				return null;
    			}
    			byte[] array = File.ReadAllBytes(text);
    			return ArrayCast.cast(array);
    		}
    		catch (Exception)
    		{
    			return null;
    		}
    	}
    
    	public static void clearAll()
    	{
    		Cout.LogError3("clean rms");
    		try
    		{
    			FileInfo[] files = new DirectoryInfo(GetiPhoneDocumentsPath() + "/").GetFiles();
    			for (int i = 0; i < files.Length; i++)
    			{
    				try
    				{
    					files[i].Delete();
    				}
    				catch (Exception ex)
    				{
    					Debug.LogWarning("Cannot delete RMS file " + files[i].Name + ": " + ex.Message);
    				}
    			}
    		}
    		catch (Exception ex2)
    		{
    			Debug.LogWarning("Cannot clear RMS: " + ex2.Message);
    		}
    	}
    
    	public static void DeleteStorage(string path)
    	{
    		try
    		{
    			File.Delete(GetiPhoneDocumentsPath() + "/" + path);
    		}
    		catch (Exception)
    		{
    		}
    	}
    }
}
