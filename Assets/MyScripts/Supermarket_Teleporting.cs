using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit;


public class Supermarket_teleporting : MonoBehaviour
{
    public UnityEvent onTeleport;

    private const int RICE = 0;
    private const int WINE = 1;
    private const int CREAM = 2;
    private const int WATER = 3;
    private const int SUGAR = 4;
    private const int PRODUCTS_NUM = 5;
    private const float XR_HANDS_HEIGHT = 1f;

    public Dictionary<int, Vector3> supermarketMap = new Dictionary<int, Vector3>()
    {
        { 0, new Vector3(-6f, XR_HANDS_HEIGHT, -6f) },    // Index 0: beginning
        { 1, new Vector3(-10f, XR_HANDS_HEIGHT, -4f) },   // Index 1: cleaningAisle
        { 2, new Vector3(-12f, XR_HANDS_HEIGHT, -4f) },   // Index 2: frozenAisle
        { 3, new Vector3(-10.5f, XR_HANDS_HEIGHT, 2f) },  // Index 3: kidsAisle
        { 4, new Vector3(-4.5f, XR_HANDS_HEIGHT, 4.5f) }, // Index 4: vegetablesArea
        { 5, new Vector3(-24f, XR_HANDS_HEIGHT, -6f) },   // Index 5: wineArea
        { 6, new Vector3(-24f, XR_HANDS_HEIGHT, -6f) },   // Index 6: alcoholArea
        { 7, new Vector3(-24f, XR_HANDS_HEIGHT, 5f) },    // Index 7: refrigeratorArea
        { 8, new Vector3(-14f, XR_HANDS_HEIGHT, -6f) },   // Index 8: emergencyExit
        { 9, new Vector3(-20f, XR_HANDS_HEIGHT, -1f) },   // Index 9: petsAisle
        { 10, new Vector3(-18f, XR_HANDS_HEIGHT, 1f) },   // Index 10: dryFoodAisle
        { 11, new Vector3(-18f, XR_HANDS_HEIGHT, 5f) },   // Index 11: snacksAisle
        { 12, new Vector3(-18f, XR_HANDS_HEIGHT, 7f) }    // Index 12: cornflakesAisle
    };

    [SerializeField]
    private TextMeshProUGUI Text;
    [SerializeField]
    private GameObject LosingCanvas;
    [SerializeField]
    private float[] timeToFind = new float[] { 50f, 100f, 100f, 100f, 100f };

    [SerializeField]
    private float Speed = 1; // Time's speed
    private float gameTimer = 0; // A variable which holds the current time
    private float[] startingTime = new float[PRODUCTS_NUM] { 0, 0, 0, 0, 0 };
    private float[] findingTime = new float[PRODUCTS_NUM] { 0, 0, 0, 0, 0 };

    private int teleportCnt = 0;
    private bool[] foundProducts = new bool[PRODUCTS_NUM] { true, true, true, true, true };
    private bool[] teleportFlags = new bool[PRODUCTS_NUM] { false, false, false, false, false };
    private Button[] moviesBtn = new Button[PRODUCTS_NUM];

    private GameObject[] sortedCanvases;
    private GameObject[] sortedInstructions;

    public ContinuousMoveProviderBase continuousMoveProvider;
    private Button okLosingBtn;
    private string[] tags = { "rice", "wine", "cream", "water", "sugar" };
    private GameObject[] canvases;
    private GameObject[] instructions;
    private int index = 0;
    private int NextLocation = 3;

    //GUI 
    private bool isHebrew = false; 

    private string instructionEnglishString = "Watch the video and collect the correct item.";
    private string[] englishTextArray = new string[]

{
    "Welcome to the Virtual Supermarket Test! To navigate the interface, hover the ray over the button you want to select.",
    "Great job! The ray is also able to pick up the correct product. Pick up the apple.",
    "You have a natural talent for this! In the next five minutes, I'll show you a navigation video, and then you'll be asked to mimic the demonstration and pick up a specific product.",
    "Pay close attention to the navigation and select the correct product accordingly. The navigation will become more challenging."
};
    private string instructionHebrewString = "יש ללחוץ על כפתור האישור כדי להתחיל את הניווט הבא.";
    private string[] HebrewTextArray = new string[]
{
    "ברוך הבא למבחן הסופרמרקט הוירטואלי! כדי לעבור בין חלונות יש לרחף באמצעות קרן הלייזר על הכפתור הנבחר.",
    ".עבודה מצוינת! הקרן גם יכולה להרים את מוצר ספציפים.נסה להרים את התפוח באותו אופן",
    "יש לך כישרון טבעי! בחמש דקות הבאות יוקרנו סרטונים המדגימים הליכה בסופר. יש לצפות בסרטונים ולחכות את המסלול. לבסוף יש להרים את המוצר הנכון",
    "יש לשים לב לניווט ולבחור את המוצר הנכון. הניווט יהפוך למאתגר יותר עם ההתקדמות בשלבים. בהצלחה!."
};
    private int i = 0;

    void Start()
    {

        Debug.Log("Persistent Data Path: " + Application.persistentDataPath);        initGrabbingObjects();
        continuousMoveProvider = GetComponent<ContinuousMoveProviderBase>();
        canvases = GameObject.FindGameObjectsWithTag("canvas");
        instructions = GameObject.FindGameObjectsWithTag("instructions");
        sortedCanvases = SortGameObjectsByLastDigit("Canvas", 5);
        sortedInstructions = SortGameObjectsByLastDigit("Instructions", 5);

        WriteCSV();
        getLosingCanvs();
        initMoviesBtnArr();
        initGrabbingObjects();
        okLosingBtnInit();
        getVideoLenght();
        initHebrewStrings();
        ChangeTextLanguage();
        


        // Deactivate every canvas
        foreach (GameObject canvas in canvases)
        {
            canvas.SetActive(false);
            //Debug.Log(canvas.name + " Inactive");
        }
        //Test prints
        Debug.Log("sortedCanvases Length: " + sortedCanvases.Length);
        Debug.Log("sortedInstructions Length: " + sortedInstructions.Length);
        WriteCSV();
    }

    
    private void initHebrewStrings()
    {
        instructionHebrewString = ReverseLinesByCharacterCount(ReverseString(instructionHebrewString), 40);
        for (i = 0; i< HebrewTextArray.Length; i++)
        {
            HebrewTextArray[i] = ReverseLinesByCharacterCount(ReverseString(HebrewTextArray[i]), 40);
            foreach (string text in HebrewTextArray)
            {
                Debug.Log(text);
            }
        }
    }

    private string ReverseLinesByCharacterCount(string input, int characterCount)
    {
        StringBuilder result = new StringBuilder();
        StringBuilder currentLine = new StringBuilder();
        string[] words = input.Split(' ');

        foreach (string word in words)
        {
            if (currentLine.Length + word.Length + 1 > characterCount)
            {
                // Prepend the current line to the result and start a new line
                result.Insert(0, currentLine.ToString().TrimEnd() + "\n");
                currentLine.Clear();
            }
            // Add the word to the current line
            currentLine.Append(word + " ");
        }

        // Add the last line
        if (currentLine.Length > 0)
        {
            result.Insert(0, currentLine.ToString().TrimEnd() + "\n");
        }

        // Return the final string with the first character trimmed if necessary
        return result.ToString().TrimEnd('\n');
    }



    private string ReverseString(string input)
    {
        // Convert the string to a character array, reverse it, and create a new string
        char[] charArray = input.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }
  

    private void getVideoLenght()
    {
        // Get all transforms in the scene
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        List<GameObject> sortedVideos = new List<GameObject>();


        foreach (Transform t in allTransforms)
        {
            // Check if the object is inactive and has the correct tag
            if (t.gameObject.CompareTag("video"))
            {
                sortedVideos.Add(t.gameObject);
            }
        }
        // Sort the found videos by the last digit in their name
        sortedVideos = sortedVideos.OrderBy(video => GetLastDigit(video.name)).ToList();

        // Update the timeToFind array with video lengths * 3
        for (int i = 0; i < sortedVideos.Count; i++)
        {
            VideoPlayer videoPlayer = sortedVideos[i].GetComponent<VideoPlayer>();
            timeToFind[i] = (float)videoPlayer.length * 3;
        }

        // Print the updated array to the console
        //Debug.Log("Updated timeToFind array:");
        //foreach (float time in timeToFind)
        //{
        //    Debug.Log(time);
        //}

    }

    // Helper method to extract the last digit from a name
    private static int GetLastDigit(string name)
    {
        // Find the last digit in the string
        char lastDigitChar = name.LastOrDefault(char.IsDigit);

        // Convert to integer
        if (int.TryParse(lastDigitChar.ToString(), out int lastDigit))
        {
            return lastDigit;
        }

        // Return -1 if no digit found
        return -1;
    }

    private void getLosingCanvs()
    {
        // Find the Main Camera GameObject
        GameObject mainCamera = GameObject.Find("Main Camera");

        if (mainCamera != null)
        {
            // Use Transform.Find to locate the LosingCanvas within the Main Camera
            LosingCanvas = mainCamera.transform.Find("LosingCanvas")?.gameObject;

            if (LosingCanvas == null)
            {
                Debug.LogError("LosingCanvas not found under Main Camera.");
            }
        }
        else
        {
            Debug.LogError("Main Camera not found in the scene.");
        }
        Debug.Log("LosingCanvas found");
    }

    GameObject[] SortGameObjectsByLastDigit(string prefix, int count)
    {
        GameObject[] objects = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            objects[i] = GameObject.Find(prefix + "_" + (i + 1));
        }

        System.Array.Sort(objects, (a, b) => {
            int lastDigitA = int.Parse(a.name.Split('_')[1]);
            int lastDigitB = int.Parse(b.name.Split('_')[1]);
            return lastDigitA.CompareTo(lastDigitB);
        });

        return objects;
    }
    private void initGrabbingObjects()
    {
        for (int i = 0; i < tags.Length; i++)
        {
            string tag = tags[i];
            // Find all GameObjects with the specified tag
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);

            // Check if any objects were found
            if (taggedObjects.Length == 0)
            {
                Debug.Log($"No objects with the tag '{tag}' found.");
            }
            else
            {
                Debug.Log("There are " + taggedObjects.Length + " " + tag + " objects.");
                // Add event listeners
                foreach (GameObject obj in taggedObjects)
                {
                    // Add event listener to XRGrabInteractable component
                    XRGrabInteractable interactable = obj.GetComponent<XRGrabInteractable>();
                    if (interactable != null)
                    {
                        interactable.onSelectEntered.AddListener((interactor) => OnObjectSelected());
                    }
                    else
                    {
                        Debug.LogWarning($"No XRGrabInteractable component found on {obj.name}");
                    }
                }
            }
        }
    }

    private void OnObjectSelected()
    {
        //if (index >= 0 && index < findingTime.Length)
        // {

        //continuousMoveProvider.enabled = true;
        //}
    }

    private void initMoviesBtnArr()
    {
        Button[] buttons = new Button[instructions.Length];

        if (buttons.Length == 0)
        {
            Debug.Log("buttons Not Found");
        }
        else
        {
            Debug.Log("Number of buttons found: " + buttons.Length);
        }

        if (instructions.Length == 0)
        {
            Debug.Log("Instructions Not Found");
        }
        else
        {
            Debug.Log("Number of Instructions found: " + instructions.Length);
        }


        for (int i = 0; i < instructions.Length; i++)
        {
            GameObject tempInstructions = instructions[i];
            Button btn = tempInstructions.GetComponentInChildren<Button>(); // Find the Button component in the children hierarchy
            if (btn != null)
            {
                //Debug.Log("Found Button on: " + btn.name);
                buttons[i] = btn;
                index = i; // Capture the current index to use in the lambda expression
                buttons[i].onClick.AddListener(() => OnOkButtonClick(btn, tempInstructions));
                //Debug.Log(" buttons[" + i + "]= " + buttons[i].name);
            }
            else
            {
                Debug.Log("Button component not found in instruction: " + tempInstructions.name);
            }

        }

        // Find and deactivate the specific instruction by name
        GameObject specificInstruction = System.Array.Find(instructions, instruction => instruction.name == "Instructions_1");
        if (specificInstruction != null)
        {
            specificInstruction.SetActive(false);
            Debug.Log("Instruction_1 found and deactivated.");
        }
        else
        {
            Debug.Log("Instruction_1 not found.");
        }
    }

    private void OnOkButtonClick(Button button, GameObject instruction)
    {
        Debug.Log("Button clicked: " + button.name);
        startingTime[teleportCnt] = gameTimer;
        Debug.Log("startingTime[teleportCnt]: " + startingTime[teleportCnt] + " teleportCnt: " + teleportCnt);
        //canvases[teleportCnt].SetActive(true);



    }

    private void OnButtonClickMovies(int buttonIndex)
    {
        if (buttonIndex < startingTime.Length)
        {
            Debug.Log("Button was clicked!");
            startingTime[buttonIndex] += Time.deltaTime;
            Debug.Log($"StartingTime[{buttonIndex}] = {startingTime[buttonIndex]}");
        }
        else
        {
            Debug.LogError("Button index out of bounds: " + buttonIndex);
        }
    }

    private void okLosingBtnInit()
    {
        Debug.Log("okLosingBtnInit");
        // Find the Button component within the LoseText GameObject
        okLosingBtn = LosingCanvas.GetComponentInChildren<Button>();
        if (okLosingBtn == null)
        {
            Debug.LogError("Button component not found in LoseText.");
            return;
        }
        // Add a listener to the button to handle click events
        okLosingBtn.onClick.AddListener(OnButtonClickLoseText);
    }

    private void OnButtonClickLoseText()
    {
        Debug.Log("Button LosingCanvas was clicked!");
        LosingCanvas.SetActive(false);
        gameObject.transform.position = supermarketMap[NextLocation];
        Debug.Log("Teleport performed");
        continuousMoveProvider.enabled = true;
        teleportCnt++;
        sortedCanvases[teleportCnt].SetActive(true);
    }

    public void languageChanging()
    {

    }

    void Update()
    {
        if (Speed > 0)
        {
            gameTimer += Time.deltaTime * Speed;
        }
        if (foundProducts[teleportCnt] == true && startingTime[teleportCnt] != 0 && findingTime[teleportCnt] == 0 && gameTimer - startingTime[teleportCnt] > timeToFind[teleportCnt])
        {
            Debug.Log("Product number " + teleportCnt + "not found.");
            Debug.Log($"teleportCnt: {teleportCnt}, startingTime[{teleportCnt}]: {startingTime[teleportCnt]}, findingTime[{teleportCnt}]: {findingTime[teleportCnt]}, gameTimer: {gameTimer}, timeToFind[{teleportCnt}]: {timeToFind[teleportCnt]}, foundProducts[{teleportCnt}]: {foundProducts[teleportCnt]}, Condition: {(startingTime[teleportCnt] != 0 && findingTime[teleportCnt] == 0 && gameTimer - startingTime[teleportCnt] > timeToFind[teleportCnt])}");

            foundProducts[teleportCnt] = false;
            continuousMoveProvider.enabled = false;
            canvases[teleportCnt].SetActive(false);
            LosingCanvas.SetActive(true);
            Debug.Log($"After execution: foundProducts[{teleportCnt}] = {foundProducts[teleportCnt]}, " +
          $"continuousMoveProvider.enabled = {continuousMoveProvider.enabled}, " +
          $"canvases[{teleportCnt}].activeSelf = {canvases[teleportCnt].activeSelf}, " +
          $"LosingCanvas.activeSelf = {LosingCanvas.activeSelf}");

        }
    }


    public void specificTeleport(int location)
    {
        if (supermarketMap.ContainsKey(location))
        {
            NextLocation = location;
            findingTime[teleportCnt] = gameTimer;
            Debug.Log($"Finding time for {tags[teleportCnt]} updated to {findingTime[teleportCnt]}");
            onTeleport?.Invoke();
            gameObject.transform.position = supermarketMap[location];
            Debug.Log("Teleport performed");
            teleportCnt++;
        }
        else
        {
            Debug.LogError("Location key not found in supermarketMap: " + location);
        }
    }

    public void WriteCSV()
    {
        // Define the file path to the persistent data path
        string filePath = Path.Combine(Application.persistentDataPath, "data.csv");

        // Write CSV file to disk
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Write headers
            writer.WriteLine("Starting Time,Finding Time,Teleport Count,Found Product");

            // Write data
            for (int i = 0; i < PRODUCTS_NUM; i++)
            {
                writer.WriteLine($"{startingTime[i]},{findingTime[i]},{teleportCnt},{foundProducts[i]}");
            }
        }

        Debug.Log($"CSV file created at: {filePath}");

        //// Now send the email with the CSV file attachment
        //using (MemoryStream memoryStream = new MemoryStream(File.ReadAllBytes(filePath)))
        //{
        //    // Configure the email client
        //    SmtpClient client = new SmtpClient("smtp.outlock.com")
        //    {
        //        Port = 587, // Ensure this is the correct port for TLS
        //        Credentials = new NetworkCredential("virtualsupermarkettest@outlock.com", "Aa12Bb12!"),
        //        EnableSsl = true,
        //        Timeout = 10000 // Increase timeout if necessary
        //    };

        //    client.EnableSsl = true; // Ensure SSL is enabled if required

        //    // Create the email message
        //    MailMessage mailMessage = new MailMessage
        //    {
        //        From = new MailAddress("virtualsupermarkettest@onmail.com"),
        //        Subject = "CSV File from Oculus Quest 2",
        //        Body = "Attached is the CSV file generated from the Oculus Quest 2 application.",
        //    };
        //    mailMessage.To.Add("recipient-email@example.com"); // Replace with the actual recipient's email

        //    // Attach the CSV file from disk
        //    memoryStream.Position = 0; // Reset the memory stream position to the beginning
        //    Attachment attachment = new Attachment(memoryStream, "data.csv", "text/csv");
        //    mailMessage.Attachments.Add(attachment);

        //    // Send the email
        //    try
        //    {
        //        client.Send(mailMessage);
        //        Debug.Log("Email sent with CSV attachment.");
        //    }
        //    catch (SmtpException ex)
        //    {
        //        Debug.LogError($"SMTP Exception: {ex.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.LogError($"Exception: {ex.Message}");
        //    }
        //}
    }

    public void ChangeTextLanguage()
    {
        Debug.Log("ChangeTextLanguage function called. isHebrew = " + isHebrew);
        i = 0;

        // Find all GameObjects in the scene, including inactive ones
        GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        // List to store found text objects
        List<GameObject> textObjects = new List<GameObject>();

        // Filter objects by the "text" tag
        foreach (GameObject obj in allGameObjects)
        {
            if (obj.CompareTag("text"))
            {
                textObjects.Add(obj);
            }
        }

        // Sort the found text objects by the last digit in their names
        var sortedTextObjects = textObjects
            .OrderBy(obj => GetLastDigit(obj.name))
            .ToList();

        // Debug information
        Debug.Log($"Number of objects with 'text' tag found: {sortedTextObjects.Count}");

        // Print the names of the sorted text objects
        foreach (GameObject obj in sortedTextObjects)
        {
            Debug.Log($"Sorted text object: {obj.name}");
        }

        if (isHebrew)
        {
            foreach (GameObject text in sortedTextObjects)
            {
                TextMeshProUGUI textMeshPro = text.GetComponent<TextMeshProUGUI>();
                string textContent = englishTextArray[i++];

                // Add spaces until the row is complete (30 characters per row)
                if (textContent.Length % 30 != 0)
                {
                    int remainingSpaces = 30 - (textContent.Length % 30);
                    textContent += new string(' ', remainingSpaces);
                }

                textMeshPro.text = textContent;
                textMeshPro.alignment = TextAlignmentOptions.Right;
            }
            foreach (GameObject instruction in sortedInstructions)
            {
                TextMeshProUGUI textMeshPro = instruction.GetComponent<TextMeshProUGUI>();
                string instructionContent = instructionEnglishString;

                // Add spaces until the row is complete (30 characters per row)
                if (instructionContent.Length % 30 != 0)
                {
                    int remainingSpaces = 30 - (instructionContent.Length % 30);
                    instructionContent += new string(' ', remainingSpaces);
                }

                textMeshPro.text = instructionContent;
                textMeshPro.alignment = TextAlignmentOptions.Right;
            }
            isHebrew = false;
        }
    }


    void SaveCSV()
    {
        // Define the file path based on the platform
        string filePath;

#if UNITY_EDITOR
        // Path for Unity Editor (on PC)
        filePath = "C:/This PC\\Quest 2\\Internal shared storage\\VST/data.csv";  // Replace with your actual path
        Debug.Log("Running in Unity Editor. File path set to: " + filePath);
#else
            // Path for Oculus Quest 2 (Android platform)
            filePath = Path.Combine("/sdcard/VST", "data.csv");
            Debug.Log("Running on Oculus Quest 2. File path set to: " + filePath);
#endif

        try
        {
            // Write CSV file to disk
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                Debug.Log("Writing headers to CSV file.");
                // Write headers
                writer.WriteLine("Starting Time,Finding Time,Teleport Count,Found Product");

                Debug.Log("Writing data to CSV file.");
                // Write data
                for (int i = 0; i < PRODUCTS_NUM; i++)
                {
                    writer.WriteLine($"{startingTime[i]},{findingTime[i]},{teleportCnt},{foundProducts[i]}");
                }
            }

            Debug.Log($"CSV file successfully created at: {filePath}");
        }
        catch (IOException ex)
        {
            Debug.LogError("Failed to write CSV file: " + ex.Message);
        }
    }
}
 








