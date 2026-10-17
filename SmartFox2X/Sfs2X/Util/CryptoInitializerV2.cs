using System;
using System.Collections.Generic;
using Sfs2X.Core;

namespace Sfs2X.Util
{
	public class CryptoInitializerV2 : ICryptoInitializer
	{
		private const string KEY_SESSION_TOKEN = "SessToken";

		private const string TARGET_SERVLET = "/BlueBox/CryptoManager";

		private SmartFox sfs;

		private bool useHttps = true;

		public CryptoInitializerV2(SmartFox sfs)
		{
			if (!sfs.IsConnected)
			{
				throw new InvalidOperationException("Cryptography cannot be initialized before connecting to SmartFoxServer!");
			}
			if (sfs.GetSocketEngine().CryptoKey != null)
			{
				throw new InvalidOperationException("Cryptography is already initialized!");
			}
			this.sfs = sfs;
		}

		public void Run()
		{
			Init();
		}

		private async void Init()
		{
			string targetUrl = (useHttps ? "https://" : "http://") + sfs.Config.Host + ":" + (useHttps ? sfs.Config.HttpsPort : sfs.Config.HttpPort) + "/BlueBox/CryptoManager";
            OnHttpError("Error " + 400 + ": " + "Not being implated");
        }

		private void OnHttpResponse(string rawData)
		{
			byte[] data = Convert.FromBase64String(rawData);
			ByteArray byteArray = new ByteArray();
			ByteArray byteArray2 = new ByteArray();
			byteArray.WriteBytes(data, 0, 16);
			byteArray2.WriteBytes(data, 16, 16);
			sfs.GetSocketEngine().CryptoKey = new CryptoKey(byteArray2, byteArray);
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["success"] = true;
			sfs.DispatchEvent(new SFSEvent(SFSEvent.CRYPTO_INIT, dictionary));
		}

		private void OnHttpError(string errorMsg)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["success"] = false;
			dictionary["errorMessage"] = errorMsg;
			sfs.DispatchEvent(new SFSEvent(SFSEvent.CRYPTO_INIT, dictionary));
		}
	}
}
