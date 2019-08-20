﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.XphotoModule;

public class PaintScene : MonoBehaviour {
    [SerializeField] private GameObject arcamera;
    [SerializeField] private Camera camera;
    [SerializeField] private ObjectManipulation objectMan;
    [SerializeField] private GameObject paintButton;
    [SerializeField] private GameObject backButton;
    [SerializeField] private Image image;
    [SerializeField] private Canvas myCanvas;

    public GameObject loadingScreen;
    private Sprite screenshot;
    private string path = "";
    List<Color32> color_palette = new List<Color32>();

    //opencv
    private WebCamTextureToMatHelper webCamTextureToMatHelper;

    // Use this for initialization
    void Start () {

    }
	
	// Update is called once per frame
	void Update () {
        
    }

    public void runPaint() {
        StartCoroutine(Paint());
    }

    private IEnumerator Paint() {
        /*
        //OPENCV
        webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

        #if UNITY_ANDROID && !UNITY_EDITOR
        // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
        webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
        #endif
        webCamTextureToMatHelper.Initialize();
        //OPENCV
        */

        objectMan.reset();
        myCanvas.enabled = false;
        screenShot();
        yield return new WaitUntil(() => File.Exists(path) == true);
        showScreenShot();
    }

    private void LoadingScreen()
    {
        // Set it to the inverse of what the current active state is
        loadingScreen.SetActive(!loadingScreen.activeSelf);
    }

    private void screenShot() {
        Debug.Log(Application.persistentDataPath);
        path = Application.persistentDataPath + "/UnalteredScene.png";
        //ScreenCapture.CaptureScreenshot("UnalteredScene.png");
        //ScreenCapture.CaptureScreenshot(path);
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        camera.targetTexture = rt;
        Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        camera.Render();
        RenderTexture.active = rt;
        texture.ReadPixels(new UnityEngine.Rect(0, 0, Screen.width, Screen.height), 0, 0);
        texture.Apply();
        byte[] shot = texture.EncodeToJPG();

        camera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        File.WriteAllBytes(path, shot);
        Debug.Log(path);
    }

    private void showScreenShot() {
        DisplayImage(path);
        image.enabled = true;
        paintButton.SetActive(false);
        backButton.SetActive(true);
        myCanvas.enabled = true;
    }

    private void DisplayImage(string path) {
        if (System.IO.File.Exists(path)) {
            path = Application.persistentDataPath + "/abcd1.jpg"; 
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(bytes);
            Texture2D orgTexture = new Texture2D(1, 1); ;
            orgTexture.LoadImage(bytes);

            if (Screen.orientation == ScreenOrientation.Portrait) {
                int height = texture.height / (texture.width / 480);
                texture = ScaleTexture(texture, 480, height);
                orgTexture = ScaleTexture(orgTexture, 480, height);
            }
            else {
                int width = texture.width / (texture.height / 480);
                texture = ScaleTexture(texture, width, 480);
                orgTexture = ScaleTexture(orgTexture, width, 480);
            }
            int pnum = 0;
            texture = changeColor(texture, pnum);
            
            //OPENCV
            Mat TextureMat = new Mat(texture.height, texture.width, CvType.CV_8UC4);
            Utils.texture2DToMat(texture, TextureMat);
            
            Mat label = new Mat(texture.height, texture.width, CvType.CV_8UC1);
            //Get KMEANS
            //TermCriteria tc = new TermCriteria(TermCriteria.EPS, 10, 1);
            //Core.kmeans(TextureMat, 20, label, tc, 10, Core.KMEANS_PP_CENTERS);

            Mat grayMat = new Mat(TextureMat.rows(), TextureMat.cols(), CvType.CV_8UC1);
            Mat gradientx = new Mat(TextureMat.rows(), TextureMat.cols(), CvType.CV_8UC1);
            Mat gradienty = new Mat(TextureMat.rows(), TextureMat.cols(), CvType.CV_8UC1);
            
            Mat rgbaMat = new Mat(texture.height, texture.width, CvType.CV_8UC4);
            Utils.texture2DToMat(texture, rgbaMat);
            
            Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
            Imgproc.Scharr(grayMat, gradientx, rgbaMat.depth(), 1, 0, 1 / 15.36);
            Imgproc.Scharr(grayMat, gradienty, rgbaMat.depth(), 0, 1, 1 / 15.36);

            //int gradient_smoothing_radius = Math.Round(Math.Max(rgbaMat.dims) / 50);
            //chosen stroke scale: 2
            //chosen gradient smoothing radius: 16
            Imgproc.GaussianBlur(gradientx, gradientx, new Size(2 * 16 + 1, 2 * 16 + 1), 0);
            Imgproc.GaussianBlur(gradienty, gradienty, new Size(2 * 16 + 1, 2 * 16 + 1), 0);

            Imgproc.medianBlur(rgbaMat, rgbaMat, 11);

            List<int> gridx = new List<int>();
            List<int> gridy = new List<int>();
            int index = 0;
            System.Random rnd = new System.Random();

            //new grid
            for (int i = 0; i < texture.width; i+=3) {
                for (int j = 0; j < texture.height; j+=3) {
                    int x = rnd.Next(-1, 2) + i;
                    int y = rnd.Next(-1, 2) + j;

                    gridy.Add(y % texture.height);
                    gridx.Add(x % texture.width);
                    index++;
                }
            }
            //shuffle grid
            int n = gridy.Count;
            while (n > 1) {
                n--;
                int k = rnd.Next(n + 1);
                int temp = gridy[k];
                gridy[k] = gridy[n];
                gridy[n] = temp;

                temp = gridx[k];
                gridx[k] = gridx[n];
                gridx[n] = temp;
            }

            int batch_size = 10000;
            Debug.Log(gridx.Count + " " + gridy.Count);
            List<Color32> pixels = new List<Color32>();
            List<Color32> orgPixels = new List<Color32>();
            double shortest = 50, longest = 0, angleShort = 0;
            for (int h = 0; h < index - 1; h += batch_size) {
                pixels = new List<Color32>();
                orgPixels = new List<Color32>();
                int endpoint = h + batch_size;
                if (endpoint > index - 1)
                    endpoint = index - 1;
                //get the color from the texture
                for (int px = h; px < endpoint; px++) {
                    Color32 cpixel = texture.GetPixel(gridx[px], (gridy[px] - (texture.height - 1)) * (-1));
                    pixels.Add(cpixel);
                    cpixel = orgTexture.GetPixel(gridx[px], (gridy[px] - (orgTexture.height - 1)) * (-1));
                    orgPixels.Add(cpixel);
                }
                int cindex = 0;
                for (int px = h; px < endpoint; px++) {
                    int x = gridx[px],
                        y = gridy[px];

                    //get color
                    Color32 cpixel;
                    //use color of pixel
                    int cprob = rnd.Next(1, 11);

                    //if(cprob <= 10) {
                    Color a, b;
                    a = orgPixels[cindex];
                    b = pixels[cindex];
                    //here lol
                    cpixel = (a+b)/2;
                    //cpixel = a;
                    //} else {
                    //if (cprob > 5) {
                    List<Color32> c_palette = color_palette;
                    c_palette.Remove(pixels[cindex]);
                    cprob = 0;
                    cprob = rnd.Next(0, c_palette.Count-1);
                    a = c_palette[cprob];
                    b = cpixel;
                    //cpixel = (a  + b )/2;
                    //cpixel = b*1.1f;
                        
                    
                    //cpixel = a;

                    //}
                    cindex++;
                    //get angle
                    double length = Math.Round(2+2 * Math.Sqrt(Math.Sqrt(gradienty.get(y, x)[0] * gradienty.get(y, x)[0] + gradientx.get(y, x)[0] * gradientx.get(y, x)[0])));
                    double angle = (180 / Math.PI) * (Math.Atan2(gradienty.get(y, x)[0], gradientx.get(y, x)[0])) + 90;
                    double lengthb = 1;
                    if (length < shortest) {
                        shortest = length;
                        angleShort = angle;
                    }
                    if(length > longest)
                        longest = length;
                    if (length > 2 && angle != 90) {
                        length /= 3;
                    } else {
                        angle += 80;
                        length = 10;
                        lengthb = 2;
                        float H, S, V;
                        Color.RGBToHSV(b, out H, out S, out V);
                        float sat = cprob * 0.01f;
                        sat += 1;
                        S *= sat;
                        cpixel = Color.HSVToRGB(H, S, V);
                    }
                    Imgproc.ellipse(rgbaMat, new Point(x, y), new Size(length, lengthb), angle, 0, 360, new Scalar(cpixel.r, cpixel.g, cpixel.b), -1, Imgproc.LINE_AA);
                }
            }
            Debug.Log("Longest : " + longest);
            Debug.Log("Shortest : " + shortest);
            Debug.Log("Angle : " + angleShort);
            Utils.matToTexture2D(rgbaMat, texture);
            
            //OPENCV
            
            Sprite sprite = Sprite.Create(texture, new UnityEngine.Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
            image.sprite = sprite;
            screenshot = sprite;
            arcamera.SetActive(false);
        }
    }

    public void back() {
        if(!path.Equals(""))
            File.Delete(path);
        path = "";
        image.enabled = false;
        backButton.SetActive(false);
        paintButton.SetActive(true);
        objectMan.reset();
        arcamera.SetActive(true);
    }

    private Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight) {
        Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, true);
        Color[] rpixels = result.GetPixels(0);
        float incX = ((float)1 / source.width) * ((float)source.width / targetWidth);
        float incY = ((float)1 / source.height) * ((float)source.height / targetHeight);
        for (int px = 0; px < rpixels.Length; px++) {
            rpixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth),
                              incY * ((float)Mathf.Floor(px / targetWidth)));
        }
        result.SetPixels(rpixels, 0);
        result.Apply();
        return result;
    }
    
    private Texture2D changeColor(Texture2D source, int pnum) {
        Color[] palette = getPalette(pnum);

        Color[] rpixels = source.GetPixels(0);
        Color mixed = new Color();
        Debug.Log(rpixels.Length);

        for (int px = 0; px < rpixels.Length; px++) {
            double lowestValue = 0;
            int colorIndex = -1;

            //First Color Detection
            for (int x = 0; x < 9; x++) {
                if(lowestValue > HSVDistanceCompare(palette[x], rpixels[px]) || colorIndex == -1) {
                    lowestValue = HSVDistanceCompare(palette[x], rpixels[px]);
                    colorIndex = x;
                }
            }
            
            mixed = palette[colorIndex];

            //Color Mixing
            for (int x = 0; x < 9; x++) {
                if (colorIndex != x) {
                    Color a = mixed,
                          b = palette[x];
                    if (lowestValue > HSVDistanceCompare((a + b) / 2, rpixels[px])) {
                        lowestValue = HSVDistanceCompare((a + b) / 2, rpixels[px]);
                        colorIndex = x;
                    }
                }
            }
            mixed = (mixed + palette[colorIndex]) / 2;
            
            rpixels[px] = mixed;
            Color32 cpixel = mixed;
            if (!color_palette.Contains(cpixel)) {
                color_palette.Add(cpixel);
            }
            //rpixels[px] *= 1.5f;
        }
        source.SetPixels(rpixels, 0);
        source.Apply();
        return source;
    }

     Color LeadWhite = new Color(0.8627f, 0.8588f, 0.8392f),
          CadiumYellow = new Color(1.0f, 0.965f, 0.0f),
          ViridianGreen = new Color(0.251f, 0.51f, 0.427f),
          EmeraldGreen = new Color(0.314f, 0.784f, 0.471f),
          FrenchUltramarine = new Color(0.071f, 0.039f, 0.561f),
          CobaltBlue = new Color(0.0f, 0.278f, 0.671f),
          AlizarinCrimson = new Color(0.89f, 0.149f, 0.212f),
          VermilionRed = new Color(0.89f, 0.259f, 0.204f),
          IvoryBlack = new Color(0.16f, 0.14f, 0.13f);

    public Color[] getPalette(int num) {
        if(num == 1) {
            Color[] palette = new Color[9];
            return palette;
        } else {
            Color[] palette = new Color[9];
            palette[0] = LeadWhite;
            palette[1] = CadiumYellow;
            palette[2] = ViridianGreen;
            palette[3] = EmeraldGreen;
            palette[4] = FrenchUltramarine;
            palette[5] = CobaltBlue;
            palette[6] = AlizarinCrimson;
            palette[7] = VermilionRed;
            palette[8] = IvoryBlack;
            return palette;
        }
        
    }

    public static double HSVDistanceCompare(Color e1, Color e2) {
        float H1, S1, V1, H2, S2, V2;
        Color.RGBToHSV(e1, out H1, out S1, out V1);
        Color.RGBToHSV(e2, out H2, out S2, out V2);
        Vector3 x = new Vector3(H1, S1, V1);
        Vector3 y = new Vector3(H2, S2, V2);

        return Vector3.Distance(x, y);
    }

    public static double ColourDistanceGreyScale(Color e1, Color e2) {
        return Math.Sqrt(Math.Pow(e2.r * 0.299f - e1.r * .3, 2) + Math.Pow(e2.g * 0.587f - e1.g * 0.587f, 2) + Math.Pow(e2.b * 0.114f - e1.b * 0.114f, 2));
    }

    public static double ColourDistance(Color e1, Color e2) {
        //return Math.Sqrt( Math.Pow(e2.r * 0.299f - e1.r *.3, 2) + Math.Pow(e2.g * 0.587f - e1.g * 0.587f, 2)+ Math.Pow(e2.b * 0.114f - e1.b * 0.114f, 2));
        return Math.Sqrt(Math.Pow(e2.r - e1.r, 2) + Math.Pow(e2.g - e1.g, 2) + Math.Pow(e2.b - e1.b, 2));
        //return (Math.Abs(e2.r - e1.r) / 256 + Math.Abs(e2.g - e1.g) / 256 + Math.Abs(e2.b - e1.b) /256) / 3 * 100;
        /* 
        float H1, S1, V1, H2, S2, V2;
        Color.RGBToHSV(e1, out H1, out S1, out V1);
        Color.RGBToHSV(e2, out H2, out S2, out V2);

        float d = Math.Abs(H1 - H2);

        if(d > 180)
            d = 360 - d;

        float B1 = (e1.r * 0.299f + e1.g * 0.587f + e1.b * 0.114f) / 255,
              B2 = (e2.r * 0.299f + e2.g * 0.587f + e2.b * 0.114f) / 255;

        //return d;

        //return Math.Abs((S1 + B1) - (S2 + B2));
        
        return Math.Abs((S1 + B1) - (S2 + B2)) + d;
        
        double cR = e1.r - e2.r,
               cG = e1.g - e2.g,
               cB = e1.b - e2.b,
               uR = e1.r + e2.r;
        return cR * cR * (2 + uR / 256) + cG * cG * 4 + cB * cB * (2 + (255 - uR) / 256);
        
        Vector3 x = new Vector3(e1.r, e1.g, e1.b);
        Vector3 y = new Vector3(e2.r, e2.g, e2.b);

        return Vector3.Distance(x, y);
        */
        }
    }
