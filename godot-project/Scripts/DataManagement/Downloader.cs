using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Godot;


namespace GodotLauncher
{
	public class Downloader
	{
		private string url;
		private WebRequest request;
		private Task<WebResponse> response;

		public Downloader(string url)
		{
			this.url = url;
		}


		public void Start()
		{
			ServicePointManager
					.ServerCertificateValidationCallback +=
				(sender, cert, chain, sslPolicyErrors) => true;

			request = WebRequest.Create(url);
			response = request.GetResponseAsync();
		}


		public string ReadText()
		{
			if (response == null) return null;
			if (response.Result == null) return null;

			var data = ReadData();
			return Encoding.UTF8.GetString(data);
		}


		public byte[] ReadData()
		{
			if (HasError) return null;
			if (request.GetResponse() is HttpWebResponse webResponse && webResponse.StatusCode != HttpStatusCode.OK)
			{
				GD.Print(webResponse.StatusCode);
				return null;
			}
			if (response.Result == null) return null;
			var stream = response.Result.GetResponseStream();
			if (stream == null) return null;

			var data = new byte[response.Result.ContentLength];
			int offset = 0;
			int bytesRead = 0;

			do
			{
				bytesRead = stream.Read(data, offset, data.Length-offset);
				offset += bytesRead;
			} while (bytesRead > 0);

			if (data.Length != offset)
				throw new Exception($"Data size didn't match. Expected {data.Length} but received {offset}");

			return data;
		}


		public long DataLength => response.Result.ContentLength;


		public bool IsDone => response != null && response.IsCompleted;
		public bool HasError => response != null && response.IsFaulted;

		public string ErrorMessage => response.Exception.Message ?? "";
	}
}