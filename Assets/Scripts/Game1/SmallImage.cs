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
    		if (imgbig == null)
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
    	}
    
    	public static void init()
    	{
    		instance = null;
    		instance = new SmallImage();
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
    			imgNew[id] = new Small(imgEmpty, id);
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
    		if (imageRaw.ContainsKey(id))
    		{
    			Image img = null;
    			imageRaw.TryGetValue(id, out img);
    			if (img != null)
    			{
    				imgNew[id] = new Small(img, id);
    			}
    			else
    			{
    				flag = true;
    			}
    		}
    		else
    		{
    			flag = true;
    		}
    		if (flag)
    		{
    			imgNew[id] = new Small(imgEmpty, id);
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
		if (lastRequestTimeById.ContainsKey(id))
		{
			return;
		}
		lastRequestTimeById[id] = mSystem.currentTimeMillis();
		//Debug.Log("SMALL_IMAGE_REQUEST id=" + id);
		Service.gI().requestIcon(id);
	}

	public static void markIconResponse(int id)
	{
		if (lastRequestTimeById.ContainsKey(id))
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
		if (imgbig == null)
		{
			Small small = imgNew[id];
    			if (small == null)
    			{
    				createImage(id);
    			}
    			else
    			{
    				g.drawRegion(small, 0, 0, mGraphics.getImageWidth(small.img), mGraphics.getImageHeight(small.img), transform, x, y, anchor);
    			}
    		}
    		else if (smallImg != null)
    		{
    			if (id >= smallImg.Length || smallImg[id][1] >= 256 || smallImg[id][3] >= 256 || smallImg[id][2] >= 256 || smallImg[id][4] >= 256)
    			{
    				Small small2 = imgNew[id];
    				if (small2 == null)
    				{
    					createImage(id);
    				}
    				else
    				{
    					small2.paint(g, transform, x, y, anchor);
    				}
    			}
    			else if (imgbig[smallImg[id][0]] != null)
    			{
    				g.drawRegion(imgbig[smallImg[id][0]], smallImg[id][1], smallImg[id][2], smallImg[id][3], smallImg[id][4], transform, x, y, anchor);
    			}
    		}
    		else if (GameCanvas.currentScreen != GameScr.gI())
    		{
    			Small small3 = imgNew[id];
    			if (small3 == null)
    			{
    				createImage(id);
    			}
    			else
    			{
    				small3.paint(g, transform, x, y, anchor);
    			}
    		}
    	}

	public static void drawSmallImageScale(mGraphics g, int id, int x, int y, int w, int h)
	{
		if (id < 0)
		{
			return;
		}
		ensureImageCapacity(id);
		if (imgbig == null)
		{
			Small small = imgNew[id];
			if (small == null)
			{
				createImage(id);
			}
			else
			{
				g.drawRegionScale(small.img, 0, 0, mGraphics.getImageWidth(small.img), mGraphics.getImageHeight(small.img), x, y, w, h);
			}
		}
		else if (smallImg != null)
		{
			if (id >= smallImg.Length || smallImg[id] == null || smallImg[id][1] >= 256 || smallImg[id][3] >= 256 || smallImg[id][2] >= 256 || smallImg[id][4] >= 256)
			{
				Small small2 = imgNew[id];
				if (small2 == null)
				{
					createImage(id);
				}
				else
				{
					g.drawRegionScale(small2.img, 0, 0, mGraphics.getImageWidth(small2.img), mGraphics.getImageHeight(small2.img), x, y, w, h);
				}
			}
			else if (imgbig[smallImg[id][0]] != null)
			{
				g.drawRegionScale(imgbig[smallImg[id][0]], smallImg[id][1], smallImg[id][2], smallImg[id][3], smallImg[id][4], x, y, w, h);
			}
		}
		else if (GameCanvas.currentScreen != GameScr.gI())
		{
			Small small3 = imgNew[id];
			if (small3 == null)
			{
				createImage(id);
			}
			else
			{
				g.drawRegionScale(small3.img, 0, 0, mGraphics.getImageWidth(small3.img), mGraphics.getImageHeight(small3.img), x, y, w, h);
			}
		}
	}
    
	public static void drawSmallImage(mGraphics g, int id, int f, int x, int y, int w, int h, int transform, int anchor)
	{
		if (id < 0)
		{
			return;
		}
		ensureImageCapacity(id);
		if (imgbig == null)
		{
    			Small small = imgNew[id];
    			if (small == null)
    			{
    				createImage(id);
    			}
    			else
    			{
    				g.drawRegion(small.img, 0, f * w, w, h, transform, x, y, anchor);
    			}
    		}
    		else if (smallImg != null)
    		{
    			if (id >= smallImg.Length || smallImg[id] == null || smallImg[id][1] >= 256 || smallImg[id][3] >= 256 || smallImg[id][2] >= 256 || smallImg[id][4] >= 256)
    			{
    				Small small2 = imgNew[id];
    				if (small2 == null)
    				{
    					createImage(id);
    				}
    				else
    				{
    					small2.paint(g, transform, f, x, y, w, h, anchor);
    				}
    			}
    			else if (smallImg[id][0] != 4 && imgbig[smallImg[id][0]] != null)
    			{
    				g.drawRegion(imgbig[smallImg[id][0]], 0, f * w, w, h, transform, x, y, anchor);
    			}
    			else
    			{
    				Small small3 = imgNew[id];
    				if (small3 == null)
    				{
    					createImage(id);
    				}
    				else
    				{
    					small3.paint(g, transform, f, x, y, w, h, anchor);
    				}
    			}
    		}
    		else if (GameCanvas.currentScreen != GameScr.gI())
    		{
    			Small small4 = imgNew[id];
    			if (small4 == null)
    			{
    				createImage(id);
    			}
    			else
    			{
    				small4.paint(g, transform, f, x, y, w, h, anchor);
    			}
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
