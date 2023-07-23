using System.Net;
using System.Text;
using Godot;


namespace GodotLauncher;

public partial class Downloader : Node
{
	private string url;
	private HttpRequest downloader;
	private HttpClient client;
	private byte[] data;
	private bool done;
	private string error;

	public Downloader(string url, Node downloaderParent)
	{
		this.url = url;

		downloaderParent.AddChild(this);
		downloader = new HttpRequest();
		downloader.UseThreads = true;
		AddChild(downloader);
		downloader.RequestCompleted += OnRequestCompleted;
	}


	void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		if (responseCode != (int)HttpClient.ResponseCode.Ok)
		{
			error = responseCode.ToString();
			return;
		}
		data = body;
		done = true;
	}


	public void Start()
	{
		var err = downloader.Request(url);
		if (err != Error.Ok)
		{
			GD.Print(err.ToString());
		}
	}


	public string ReadText()
	{
		return Encoding.UTF8.GetString(data);
	}


	public byte[] ReadData()
	{
		return data;
	}

	public bool IsDone => done;
	public float SizeInMb => downloader.GetDownloadedBytes() / (1024f * 1024f);
	public bool HasError => !string.IsNullOrEmpty(error);
	public string ErrorMessage => error;
}