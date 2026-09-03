using System;
using System.Collections.Generic;
using Game1.Assets.src.e;
using UnityEngine;

namespace Game1
{
    
    public class SmallImage
    {
    	public static int[][] smallImg;
    
    	public static SmallImage instance;
    
    	public static Image[] imgbig;
    
    	public static Small[] imgNew;
    
    	public static MyVector vKeys = new MyVector();
    
    	public static Image imgEmpty = null;
    
    	public static sbyte[] newSmallVersion;
    
    	public static int smallCount;
    
    	public static int maxSmall;
    
	public static Dictionary<int, Image> imageRaw = new Dictionary<int, Image>();

	private const int IMAGE_CAPACITY_CHUNK = 512;

	private static readonly Dictionary<int, long> lastRequestTimeById = new Dictionary<int, long>();
    
    	public SmallImage()
    	{
    		readImage();
    	}
    
    	public static void loadBigRMS()
    	{
    		if (imgbig == null || imgbig.Length < 5 || imgbig[0] == null)
    		{
    			imgbig = new Image[5]
    			{
    				GameCanvas.loadImageRMS("/img/Big0.png"),
    				GameCanvas.loadImageRMS("/img/Big1.png"),
    				GameCanvas.loadImageRMS("/img/Big2.png"),
    				GameCanvas.loadImageRMS("/img/Big3.png"),
    				GameCanvas.loadImageRMS("/img/Big4.png")
    			};
    		}
    	}
    
    	public static void loadBigImage()
    	{
    		imgEmpty = Image.createRGBImage(new int[1], 1, 1, bl: true);
    		loadBigRMS();
    	}
    
    	public static void init()
    	{
    		instance = null;
    		instance = new SmallImage();
    		loadBigRMS();
    	}
    
    	public void readImage()
    	{
    		int num = 0;
    		try
    		{
    			DataInputStream dataInputStream = new DataInputStream(Rms.loadRMS("NR_image"));
    			short num2 = dataInputStream.readShort();
    			smallImg = new int[num2][];
    			for (int i = 0; i < smallImg.Length; i++)
    			{
    				smallImg[i] = new int[5];
    			}
    			for (int j = 0; j < num2; j++)
    			{
    				num++;
    				smallImg[j][0] = dataInputStream.readUnsignedByte();
    				smallImg[j][1] = dataInputStream.readShort();
    				smallImg[j][2] = dataInputStream.readShort();
    				smallImg[j][3] = dataInputStream.readShort();
    				smallImg[j][4] = dataInputStream.readShort();
    			}
    		}
    		catch (Exception ex)
    		{
    			Cout.LogError3("Loi readImage: " + ex.ToString() + "i= " + num);
    		}
    	}
    
	public static void clearHastable()
	{
	}

	public static void ensureImageCapacity(int id)
	{
		if (id < 0)
		{
			return;
		}
		int capacity = ((id / IMAGE_CAPACITY_CHUNK) + 1) * IMAGE_CAPACITY_CHUNK;
		if (imgNew == null)
		{
			//Debug.Log("SMALL_IMAGE_CHUNK: Allocated initial chunk. Capacity: " + capacity + " (IDs 0 to " + (capacity - 1) + ") for requested id: " + id);
			imgNew = new Small[capacity];
		}
		else if (id >= imgNew.Length)
		{
			int oldCapacity = imgNew.Length;
			Array.Resize(ref imgNew, capacity);
			//Debug.Log("SMALL_IMAGE_CHUNK: Expanded capacity from " + oldCapacity + " to " + capacity + " (IDs " + oldCapacity + " to " + (capacity - 1) + ") for requested id: " + id);
		}
		if (newSmallVersion != null && id >= newSmallVersion.Length)
		{
			Array.Resize(ref newSmallVersion, capacity);
		}
		if (id + 1 > maxSmall)
		{
			maxSmall = id + 1;
		}
	}

	public static void ensureImageSlot(int id)
	{
		ensureImageCapacity(id);
		if (imgEmpty == null)
		{
			loadBigImage();
		}
		if (id >= 0 && imgNew[id] == null)
		{
			imgNew[id] = new Small(imgEmpty, id);
		}
	}

	public static void createImage(int id)
	{
		if (id < 0)
		{
			return;
		}
		ensureImageCapacity(id);
		if (mGraphics.zoomLevel == 1)
		{
			Image image = GameCanvas.loadImage("/SmallImage/Small" + id + ".png");
			if (image != null)
			{
				imgNew[id] = new Small(image, id);
				return;
			}
			if (imgNew[id] == null)
			{
				imgNew[id] = new Small(imgEmpty, id);
			}
			requestIconIfNeeded(id);
			return;
		}
		Image image2 = GameCanvas.loadImage("/SmallImage/Small" + id + ".png");
		if (image2 != null)
		{
			imgNew[id] = new Small(image2, id);
			return;
		}
		bool flag = false;
		lock (imageRaw)
		{
			if (imageRaw.TryGetValue(id, out Image img) && img != null)
			{
				imgNew[id] = new Small(img, id);
			}
			else
			{
				flag = true;
			}
		}
		if (flag)
		{
			if (imgNew[id] == null)
			{
				imgNew[id] = new Small(imgEmpty, id);
			}
			requestIconIfNeeded(id);
		}
	}

	public static bool isRealImageLoaded(int id)
	{
		if (id < 0 || imgNew == null || id >= imgNew.Length || imgNew[id] == null || imgNew[id].img == null)
		{
			return false;
		}
		return mGraphics.getImageWidth(imgNew[id].img) > 1 && mGraphics.getImageHeight(imgNew[id].img) > 1;
	}

	public static void requestIconIfNeeded(int id)
	{
		if (id < 0 || isRealImageLoaded(id))
		{
			return;
		}
		long now = mSystem.currentTimeMillis();
		lock (lastRequestTimeById)
		{
			if (lastRequestTimeById.TryGetValue(id, out long lastTime))
			{
				if (now - lastTime < 3000)
				{
					return;
				}
			}
			lastRequestTimeById[id] = now;
		}
		Service.gI().requestIcon(id);
	}

	public static void markIconResponse(int id)
	{
		lock (lastRequestTimeById)
		{
			lastRequestTimeById.Remove(id);
		}
	}
    
	public static void drawSmallImage(mGraphics g, int id, int x, int y, int transform, int anchor)
	{
		if (id < 0)
		{
			return;
		}
		ensureImageCapacity(id);
		if (imgbig == null || imgbig.Length < 5 || imgbig[0] == null)
		{
			loadBigRMS();
		}
		if (smallImg == null && instance != null)
		{
			instance.readImage();
		}
		if (smallImg != null && id < smallImg.Length && smallImg[id] != null)
		{
			int bigIdx = smallImg[id][0];
			if (smallImg[id][1] < 256 && smallImg[id][3] < 256 && smallImg[id][2] < 256 && smallImg[id][4] < 256 &&
			    imgbig != null && bigIdx >= 0 && bigIdx < imgbig.Length && imgbig[bigIdx] != null)
			{
				g.drawRegion(imgbig[bigIdx], smallImg[id][1], smallImg[id][2], smallImg[id][3], smallImg[id][4], transform, x, y, anchor);
				return;
			}
		}
		Small small = imgNew[id];
		if (small == null || !isRealImageLoaded(id))
		{
			createImage(id);
			small = imgNew[id];
		}
		if (small != null)
		{
			small.paint(g, transform, x, y, anchor);
		}
	}

	public static void drawSmallImageScale(mGraphics g, int id, int x, int y, int w, int h)
	{
		if (id < 0)
		{
			return;
		}
		ensureImageCapacity(id);
		if (imgbig == null || imgbig.Length < 5 || imgbig[0] == null)
		{
			loadBigRMS();
		}
		if (smallImg == null && instance != null)
		{
			instance.readImage();
		}
		if (smallImg != null && id < smallImg.Length && smallImg[id] != null)
		{
			int bigIdx = smallImg[id][0];
			if (smallImg[id][1] < 256 && smallImg[id][3] < 256 && smallImg[id][2] < 256 && smallImg[id][4] < 256 &&
			    imgbig != null && bigIdx >= 0 && bigIdx < imgbig.Length && imgbig[bigIdx] != null)
			{
				g.drawRegionScale(imgbig[bigIdx], smallImg[id][1], smallImg[id][2], smallImg[id][3], smallImg[id][4], x, y, w, h);
				return;
			}
		}
		Small small = imgNew[id];
		if (small == null || !isRealImageLoaded(id))
		{
			createImage(id);
			small = imgNew[id];
		}
		if (small != null)
		{
			g.drawRegionScale(small.img, 0, 0, mGraphics.getImageWidth(small.img), mGraphics.getImageHeight(small.img), x, y, w, h);
		}
	}
    
	public static void drawSmallImage(mGraphics g, int id, int f, int x, int y, int w, int h, int transform, int anchor)
	{
		if (id < 0)
		{
			return;
		}
		ensureImageCapacity(id);
		if (imgbig == null || imgbig.Length < 5 || imgbig[0] == null)
		{
			loadBigRMS();
		}
		if (smallImg == null && instance != null)
		{
			instance.readImage();
		}
		if (smallImg != null && id < smallImg.Length && smallImg[id] != null)
		{
			int bigIdx = smallImg[id][0];
			if (bigIdx != 4 && bigIdx >= 0 && bigIdx < imgbig.Length && imgbig[bigIdx] != null &&
			    smallImg[id][1] < 256 && smallImg[id][3] < 256 && smallImg[id][2] < 256 && smallImg[id][4] < 256)
			{
				g.drawRegion(imgbig[bigIdx], 0, f * w, w, h, transform, x, y, anchor);
				return;
			}
		}
		Small small = imgNew[id];
		if (small == null || !isRealImageLoaded(id))
		{
			createImage(id);
			small = imgNew[id];
		}
		if (small != null)
		{
			small.paint(g, transform, f, x, y, w, h, anchor);
		}
	}
    
    	public static void update()
    	{
    		int num = 0;
    		if (GameCanvas.gameTick % 1000 != 0)
    		{
    			return;
    		}
    		for (int i = 0; i < imgNew.Length; i++)
    		{
    			if (imgNew[i] != null)
    			{
    				num++;
    				imgNew[i].update();
    				smallCount++;
    			}
    		}
    	}
    }
}
