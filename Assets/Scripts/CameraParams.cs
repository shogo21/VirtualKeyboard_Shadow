public class CameraParams
{
    public static float[,] cameraMatrix = new float[3, 3]{
        {410.66680666f,0f,306.11782904f},
        {0f, 410.45769523f, 267.58754931f},
        {0f, 0f, 1f}
    };
    public static float[,] distCoeffs = new float[1, 5]{{
        -0.399426877f,
        0.180104814f,
        0.00109398412f,
        0.0000503861785f,
        -0.0400962344f
    }};
    public static float[,] optimalNewCameraMatrix = new float[3, 3]{
        {234.8145752f, 0f, 315.74977981f},
        {0f, 228.46601868f, 266.84868719f},
        {0f, 0f, 1f}
    };

    public static int[] regionOfInterest = new int[4]{
        141, 118, 366, 267
    };
}