using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Class to Import .anim Brawl Animations exported from BrawlBox;
/// </summary>
public class BrawlAnimationImporter : MonoBehaviour
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Constant
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// The frame rate of animation;
    /// </summary>
    private const float frameRate = 60;

    /// <summary>
    /// The folder where animations are saved;
    /// Inside the Assets folder;
    /// </summary>
    private const string animationsFolder = "UnityBrawlAnimationImporter";

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Paths
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// The folder where all animations are located;
    /// </summary>
    private static string startFolder;

    /// <summary>
    /// The text file containing the bone paths;
    /// </summary>
    private static string bonePathFile = Application.dataPath + Path.DirectorySeparatorChar + animationsFolder + Path.DirectorySeparatorChar + "BonePaths.txt";

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Data Structures
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Dictionary of relative paths for bones;
    /// Key: The bone name;
    /// Value: the relative path for that bone;
    /// </summary>
    private static Dictionary<string, string> BonePathDictionary = new Dictionary<string, string>();

    /// <summary>
    /// List of all BrawlAnimations that were created;
    /// </summary>
    private static List<BrawlAnimation> BrawlAnimations = new List<BrawlAnimation>();

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [UnityEditor.MenuItem("Brawl Animations/Import")]
    public static void ImportAniamtions()
    {
        //Clear old shit
        BonePathDictionary.Clear();
        BrawlAnimations.Clear();

        //Select the folder
        startFolder = EditorUtility.OpenFolderPanel("Select the folder which contains the aniamtions to import", Application.dataPath + Path.DirectorySeparatorChar + animationsFolder, "");

        //Create the bone paths
        CreateBonePathDictionary(bonePathFile);

        //Create BrawlAnimation objects;
        string[] animPaths = CreateClipNameArray(startFolder);
        foreach (string animFilePath in animPaths)
        {
            BrawlAnimations.Add(new BrawlAnimation(animFilePath));
        }

        //Save the Files;
        SaveAnimationFiles();
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Directories and Shit
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Creates the name of all the animation clips in a folder;
    /// </summary>
    /// <param name="startFolder">The folder where the clips are located</param>
    /// <returns></returns>
    private static string[] CreateClipNameArray(string startFolder)
    {
        List<string> animationPath = new List<string>(); //List of aniamtion files in there;

        string[] animFiles = Directory.GetFiles(startFolder); //The array of files for the .anim stuff

        //Build the list of valid anim files;
        for (int i = 0; i < animFiles.Length; i++)
        {
            if (Path.GetExtension(animFiles[i]) == ".anim")
            {
                animationPath.Add(animFiles[i]);
            }
        }

        return animationPath.ToArray();
    }

    /// <summary>
    /// Creates the BonePath dictionary;
    /// </summary>
    /// <param name="filePath"></param>
    private static void CreateBonePathDictionary(string filePath)
    {
        if (!File.Exists(filePath)) { Debug.LogError("ERROR: BonePaths.txt is missing! Please read the readme :)"); }

        StreamReader defintionFile = new StreamReader(filePath); string currentLine;
        while ((currentLine = defintionFile.ReadLine()) != null) //Read the file in line by line
        {
            string[] split = currentLine.Split('=');
            BonePathDictionary.Add(split[0], split[1]);
        }

        defintionFile.Close();
    }

    /// <summary>
    /// Save all the animation files to a given folder in a Unity format;
    /// </summary>
    private static void SaveAnimationFiles()
    {
        Directory.CreateDirectory(Application.dataPath + Path.DirectorySeparatorChar + animationsFolder + Path.DirectorySeparatorChar + "Converted");
        foreach (BrawlAnimation animation in BrawlAnimations)
        {
            string filePath = "Assets" + Path.DirectorySeparatorChar + animationsFolder + Path.DirectorySeparatorChar + "Converted" + Path.DirectorySeparatorChar + animation.animationName + ".anim";
            AssetDatabase.CreateAsset(animation.CreateAniamtionClip(), filePath);
            Debug.Log(string.Format("Saved animation '{0}' at location '{1}'", animation.animationName, filePath));
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Classes
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Represents a """Maya""" Brawl animation;
    /// Read in from a .anim file;
    /// </summary>
    private class BrawlAnimation
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Fields
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of this animation;
        /// </summary>
        internal string animationName;

        /// <summary>
        /// The end of this clip, in seconds;
        /// </summary>
        private float endTime;

        /// <summary>
        /// The dictionary of bones and animations for this clip;
        /// Key: BoneName
        /// Value: All the Aniamtions for that bone;
        /// </summary>
        private Dictionary<string, BoneAnimationWrapper> boneAnimations = new Dictionary<string, BoneAnimationWrapper>();

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor;
        /// Given the path to a Brawl animation, creates it;
        /// </summary>
        /// <param name="animationFilePath"></param>
        internal BrawlAnimation(string animationFilePath)
        {
            animationName = Path.GetFileNameWithoutExtension(animationFilePath);

            StreamReader animFile = new StreamReader(animationFilePath); string currentLine;
            while ((currentLine = animFile.ReadLine()) != null) //Read the file in line by line
            {
                if (currentLine.StartsWith("endTime"))
                {
                    currentLine = currentLine.TrimEnd(';');
                    string[] splitLine = currentLine.Split(' '); //Split along the spaces

                    float endFrame = float.Parse(splitLine[1]); //Parse the end frame
                    endFrame--; //Decrement it by one

                    endTime = endFrame / frameRate; //Convert the frame to time;
                }
                else if (currentLine.StartsWith("anim") && (!currentLine.StartsWith("animVersion") && !currentLine.StartsWith("animData"))) //Parse a single BoneAnimation
                {
                    //Line format: anim translate.translateX TransformType BoneName
                    string[] splitLine = currentLine.Split(' '); //Split along the spaces

                    //Create the bone keyframes thing;
                    string boneName = splitLine[3];
                    string transformType = MayaToUnityTransformTypeConversion[splitLine[2]];
                    BoneAnimation boneAnimation = new BoneAnimation(transformType, endTime);

                    //Get to the actual bone data;
                    currentLine = animFile.ReadLine();
                    currentLine = animFile.ReadLine();
                    currentLine = animFile.ReadLine();
                    currentLine = animFile.ReadLine();
                    currentLine = animFile.ReadLine();
                    currentLine = animFile.ReadLine();
                    currentLine = animFile.ReadLine();
                    currentLine = animFile.ReadLine();
                    currentLine = currentLine.Trim(); //Trims the line

                    //Keyframe Data Building
                    while (currentLine.Length > 0 && char.IsDigit(currentLine[0])) //Iterate through all the keyframes;
                    {
                        string[] keyFrameData = currentLine.Split(' '); //Line Format: <StartFrame> <Value> auto auto <N1> <N2> <N3>

                        float frame = float.Parse(keyFrameData[0]); //time is a frame here
                        frame = frame - 1; //Adjust for the start time being incremented 1 forward;
                        float time = frame / frameRate; //time is now the frame/ framerate, which makes it the frame in terms of seconds

                        float value = float.Parse(keyFrameData[1]); //Get the value of the bone at this keyframe;

                        if (transformType == "localPosition.x") { value = -value; } //No idea why I have this line, it must have solved some kind of weird flipping issue; I should move it to the ConvertBoneAnimationsToUnity method, but I don't wanna!

                        boneAnimation.AddKeyFrame(time, value); //Add the keyframe

                        //Go to the next line
                        currentLine = animFile.ReadLine();
                        currentLine = currentLine.Trim();
                    }

                    //get the wrapper
                    BoneAnimationWrapper wrapper;
                    if (!boneAnimations.TryGetValue(boneName, out wrapper))
                    {
                        wrapper = new BoneAnimationWrapper(boneName, endTime);
                        boneAnimations.Add(boneName, wrapper);
                    }

                    wrapper.Add(transformType, boneAnimation); //Add the boneaniamtion to teh wrapper
                }
            }
            animFile.Close();
        }

        /// <summary>
        /// Creates an animation clip from the BoneAnaimationWrapper;
        /// </summary>
        /// <returns></returns>
        internal AnimationClip CreateAniamtionClip()
        {
            AnimationClip clip = new AnimationClip();
            foreach (BoneAnimationWrapper wrapper in boneAnimations.Values) //For every bone in this animation...
            {
                wrapper.AddCurves(clip);
            }

            return clip;
        }
    }

    /// <summary>
    /// Stores all the AnimationData for a specific bone;
    /// </summary>
    private class BoneAnimationWrapper
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Properties
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The relative path for the bone;
        /// </summary>
        internal string relativePath
        {
            get
            {
                string path;
                if (!BonePathDictionary.TryGetValue(boneName, out path)) { Debug.LogError("ERROR: Bone Path Dictioanry does not contain a definition for " + boneName + "!"); }
                return path;
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Fields
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private float endTime;

        internal string boneName;

        internal BoneAnimation posX;
        internal BoneAnimation posY;
        internal BoneAnimation posZ;

        internal BoneAnimation rotX;
        internal BoneAnimation rotY;
        internal BoneAnimation rotZ;

        internal BoneAnimation scaleX;
        internal BoneAnimation scaleY;
        internal BoneAnimation scaleZ;

        internal BoneAnimation quatW;
        internal BoneAnimation quatX;
        internal BoneAnimation quatY;
        internal BoneAnimation quatZ;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Constructor
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor;
        /// </summary>
        /// <param name="boneName"></param>
        internal BoneAnimationWrapper(string boneName, float endTime)
        {
            this.boneName = boneName;
            this.endTime = endTime;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Adding
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds an animation property for the bone;
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="animation"></param>
        internal void Add(string propertyType, BoneAnimation animation)
        {
            switch (propertyType)
            {
                case "localPosition.x":
                    posX = animation;
                    break;
                case "localPosition.y":
                    posY = animation;
                    break;
                case "localPosition.z":
                    posZ = animation;
                    break;
                case "localEuler.x":
                    rotX = animation;
                    break;
                case "localEuler.y":
                    rotY = animation;
                    break;
                case "localEuler.z":
                    rotZ = animation;
                    break;
                case "localScale.x":
                    scaleX = animation;
                    break;
                case "localScale.y":
                    scaleY = animation;
                    break;
                case "localScale.z":
                    scaleZ = animation;
                    break;
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Conversion
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the curves from the BoneAnimations to a given animation clip;
        /// </summary>
        /// <param name="clip"></param>
        internal void AddCurves(AnimationClip clip)
        {
            //Debug.LogFormat("Adding Curves for {0}", boneName);

            ConvertToQuaternionRotations();

            //Set the curves for the position
            if (posX != null) clip.SetCurve(relativePath, typeof(Transform), posX.propertyName, posX.GetFinalCurve());
            if (posY != null) clip.SetCurve(relativePath, typeof(Transform), posY.propertyName, posY.GetFinalCurve());
            if (posZ != null) clip.SetCurve(relativePath, typeof(Transform), posZ.propertyName, posZ.GetFinalCurve());

            //Set the curves for the rotation
            if (quatW != null) clip.SetCurve(relativePath, typeof(Transform), quatW.propertyName, quatW.GetFinalCurve());
            if (quatX != null) clip.SetCurve(relativePath, typeof(Transform), quatX.propertyName, quatX.GetFinalCurve());
            if (quatY != null) clip.SetCurve(relativePath, typeof(Transform), quatY.propertyName, quatY.GetFinalCurve());
            if (quatZ != null) clip.SetCurve(relativePath, typeof(Transform), quatZ.propertyName, quatZ.GetFinalCurve());

            //Set the curves for the scale
            if (scaleX != null) clip.SetCurve(relativePath, typeof(Transform), scaleX.propertyName, scaleX.GetFinalCurve());
            if (scaleY != null) clip.SetCurve(relativePath, typeof(Transform), scaleY.propertyName, scaleY.GetFinalCurve());
            if (scaleZ != null) clip.SetCurve(relativePath, typeof(Transform), scaleZ.propertyName, scaleZ.GetFinalCurve());
        }

        /// <summary>
        /// Converts the Euler rotations in Maya to Unity quaternions;
        /// This is necessary for proper interpolation;
        /// </summary>
        private void ConvertToQuaternionRotations()
        {
            if (rotX == null && rotY == null && rotZ == null) return;

            //Null Checks and such;
            BoneAnimation rotationX;
            BoneAnimation rotationY;
            BoneAnimation rotationZ;
            if (rotX == null) { rotationX = new BoneAnimation("localEuler.x", endTime); rotationX.AddKeyFrame(0, 0); } else { rotationX = rotX; }
            if (rotY == null) { rotationY = new BoneAnimation("localEuler.y", endTime); rotationY.AddKeyFrame(0, 0); } else { rotationY = rotY; }
            if (rotZ == null) { rotationZ = new BoneAnimation("localEuler.z", endTime); rotationZ.AddKeyFrame(0, 0); } else { rotationZ = rotZ; }

            //Add all the keyframe times from the old rotations
            HashSet<float> keyFrameTimes = new HashSet<float>(); //List of ALL keyframe times;
            foreach (Keyframe keyframe in rotationX.curve.keys) { if (!keyFrameTimes.Contains(keyframe.time)) { keyFrameTimes.Add(keyframe.time); } }
            foreach (Keyframe keyframe in rotationY.curve.keys) { if (!keyFrameTimes.Contains(keyframe.time)) { keyFrameTimes.Add(keyframe.time); } }
            foreach (Keyframe keyframe in rotationZ.curve.keys) { if (!keyFrameTimes.Contains(keyframe.time)) { keyFrameTimes.Add(keyframe.time); } }

            //Create new New BoneAnimations for the quaternion rotations;
            quatW = new BoneAnimation("localRotation.w", endTime);
            quatX = new BoneAnimation("localRotation.x", endTime);
            quatY = new BoneAnimation("localRotation.y", endTime);
            quatZ = new BoneAnimation("localRotation.z", endTime);

            //Add keyframes to the new quaternion animations;
            foreach (float time in keyFrameTimes)
            {
                //Convert the values into a quaternion at the given time;
                Quaternion quat = MayaRotationToUnity(rotationX.curve.Evaluate(time), rotationY.curve.Evaluate(time), rotationZ.curve.Evaluate(time));

                //Create a new keyframe set
                quatW.AddKeyFrame(time, quat.w);
                quatX.AddKeyFrame(time, quat.x);
                quatY.AddKeyFrame(time, quat.y);
                quatZ.AddKeyFrame(time, quat.z);
            }
        }
    }

    /// <summary>
    /// A classs which represents the animation of a single property of a bone;
    /// </summary>
    private class BoneAnimation
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Properties
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The property Name;
        /// the property that is being changed;
        /// </summary>
        internal string propertyName
        {
            get
            {
                return transformType;
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Fields
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        internal AnimationCurve curve = new AnimationCurve();
        internal string transformType; //The type of the transform or whatever
        private float endTime = 0; //The length of the clip in seconds

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Constructors
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor;
        /// </summary>
        /// <param name="boneName"></param>
        /// <param name="transformType"></param>
        internal BoneAnimation(string transformType, float endTime)
        {
            this.transformType = transformType;
            this.endTime = endTime;
        }

        /// <summary>
        /// Adds a new Keyframe;
        /// </summary>
        /// <param name="time"></param>
        /// <param name="value"></param>
        internal void AddKeyFrame(float time, float value)
        {
            curve.AddKey(time, value); //Add the keyframe to the BoneAnimation
        }

        internal AnimationCurve GetFinalCurve()
        {
            if (curve.keys.Length == 1)
            {
                curve.AddKey(endTime, curve.keys[0].value);
            }
            return curve;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Maya Conversion Stuff
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Converts a Maya rotation to a Unity Quaternion rotation;
    /// Converts handedness;
    /// Taken from: https://forum.unity3d.com/threads/right-hand-to-left-handed-conversions.80679/
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    private static Quaternion MayaRotationToUnity(float x, float y, float z)
    {
        Vector3 flippedRotation = new Vector3(x, -y, -z);
        Quaternion qx = Quaternion.AngleAxis(flippedRotation.x, Vector3.right);
        Quaternion qy = Quaternion.AngleAxis(flippedRotation.y, Vector3.up);
        Quaternion qz = Quaternion.AngleAxis(flippedRotation.z, Vector3.forward);
        Quaternion qq = qz * qy * qx;
        return qq;
    }

    /// <summary>
    /// Converts a Maya transform type into a Unity transform type;
    /// </summary>
    private static Dictionary<string, string> MayaToUnityTransformTypeConversion = new Dictionary<string, string>()
    {
        { "translateX", "localPosition.x" },
        { "translateY", "localPosition.y" },
        { "translateZ", "localPosition.z" },
        { "rotateX", "localEuler.x" },
        { "rotateY", "localEuler.y" },
        { "rotateZ", "localEuler.z" },
        { "scaleX", "localScale.x" },
        { "scaleY", "localScale.y" },
        { "scaleZ", "localScale.z" },
    };
}
