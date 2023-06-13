using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrashGameController : MonoBehaviour
{
    public TMP_InputField walletInput;
    public TMP_InputField wagerInput;
    public TMP_InputField rtpInput;
    public TMP_Text messageDisplay;
    public TMP_Text walletDisplay;
    public Button playButton;
    public Button stopButton;
    public Button toggleHistoryButton;
    public TMP_Text winningHistoryDisplay;
    public TMP_InputField autoPlayInput;
    public TMP_InputField autoCollectInput;
    public Button autoPlayButton;
    // public GameObject winEffectPrefab; to delete

    // DropDown Amount Selector for Wager
    public Button incrementButton;
    public Button decrementButton;
    public TMP_Dropdown betAmountDropdown;
    public TMP_InputField betAmountInputField;

    public Image playButtonImage;
    public Image stopButtonImage;
    // Coin Image reference
    public Image coinImage;
    public Sprite playSprite;
    public Sprite stopSprite;

    // Multiplier Numbers as images
    public Sprite[] numberSprites = new Sprite[11];
    public Image[] multiplierImages;

    // Declaring the increment and decrement steps
    private int incrementStep;
    private int decrementStep;
    public UnityEngine.UI.Slider multiplierSlider; // Slider values from Multiplier declaration
    //DropDown Bet Amount Pick/Selection
    public Button ShowDropdownButton;
    public GameObject BetAmountDropdown; // Assuming your dropdown is a GameObject

    private float wallet;
    private float wager;
    private float multiplier;
    private bool gameRunning = false;
    private float baseMultiplierIncrement = 0.01f; //to delete
    private List<float> winningHistory = new List<float>();
    private bool isHistoryDisplayed = false;
    // Declare local variables for wager and wallet for the AutoPlayGame method
    private float autoPlayWallet;
    private float autoPlayWager;
    private Color originalTextColor; //to delete
    private Vector3 originalTextScale; //to delete
    // Define a custom gold color
    private Color gold = new Color(1, 0.8431372f, 0);

    // Animation - Fooballer Character and Ball
    private Animator animator;
    public GameObject ball;
    public float juggleRadius = 2.0f;
    public string juggleKey = "space";


    [SerializeField] private float rtp = 80; // Percentage RTP. Default is 80%
    [SerializeField] private float depositAmount; //= 1000f;  Total deposited by the player

    private float _moneyOutThere; // This will hold the amount to be won or lost by users
    private float _houseEarnings; // This will hold the amount that the house retains

    void Start()
    {

        // betAmounts dropdown values
        int[] betAmounts = { 10, 20, 50, 100, 200 };

        // Clear current options
        betAmountDropdown.options.Clear();
        // House earnings
        UpdateRTP();
        // Create a list for new dropdown options
        List<TMP_Dropdown.OptionData> newOptions = new List<TMP_Dropdown.OptionData>();

        // Create new options
        for (int i = 0; i < betAmounts.Length; i++)
        {
            newOptions.Add(new TMP_Dropdown.OptionData(betAmounts[i].ToString()));
        }

        // Add options to Dropdown
        betAmountDropdown.options = newOptions;

        // Add a listener for dropdown value changes
        betAmountDropdown.onValueChanged.AddListener(OnBetAmountChanged);

        playButtonImage.sprite = playSprite; // start with the play button
        playButtonImage.gameObject.GetComponent<Button>().onClick.AddListener(StartGame);

        // Link UI elements to their logic
        //  playButtonImage.gameObject.GetComponent<Button>().onClick.AddListener(() => StartGame()); to delete,repeated
        stopButtonImage.gameObject.GetComponent<Button>().onClick.AddListener(() => StopGame());
        toggleHistoryButton.onClick.AddListener(ToggleWinningHistory);
        autoPlayButton.onClick.AddListener(AutoPlayGame);

        // Initially, the stop button should not be interactable
        stopButtonImage.gameObject.GetComponent<Button>().interactable = false;

        // Load your number sprites
        for (int i = 0; i < 10; i++)
        {
            numberSprites[i] = Resources.Load<Sprite>("NumberSprites/" + i);
            numberSprites[10] = Resources.Load<Sprite>("NumberSprites/point");
        }

        // Hide all the multiplier images initially
        foreach (Image image in multiplierImages)
        {
            image.gameObject.SetActive(false);
        }

        // Hide the winning history
        winningHistoryDisplay.text = "";
        coinImage.gameObject.SetActive(false);  // hide the coin image initially

        // Register the listener for the increment and decrement buttons click event
        incrementButton.onClick.AddListener(IncrementBetAmount);
        decrementButton.onClick.AddListener(DecrementBetAmount);

        // Set the initial bet amount based on the dropdown
        OnBetAmountChanged(betAmountDropdown.value);

        // Football Character and Ball Animation
        {
            animator = GetComponent<Animator>();
        }

        // Initially hide the dropdown
        BetAmountDropdown.SetActive(false);

        // Add listener for button click
        ShowDropdownButton.onClick.AddListener(ToggleDropdownVisibility);
    }

    public void AutoSpin()
    {
        if (!isSpinning && int.TryParse(spinsInputField.text, out int numberOfSpins)
            && float.TryParse(betAmountInputField.text, out float betAmount)
            && float.TryParse(autoCollectInputField.text, out float autoCollectAmount))
        {
            StartCoroutine(AutoSpinCoroutine(numberOfSpins, betAmount, autoCollectAmount));
        }
        else
        {
            // Handle invalid input or already spinning
            Debug.LogError("Invalid input or already spinning!");
        }
    }

    private IEnumerator AutoSpinCoroutine(int numberOfSpins, float betAmount, float autoCollectAmount)
    {
        for (int i = 0; i < numberOfSpins; i++)
        {
            if (!isSpinning)
            {
                StartGame(betAmount);

                while (isSpinning && currentMultiplier < autoCollectAmount)
                {
                    yield return null; // wait for next frame
                }

                if (isSpinning)
                {
                    StopGame(); // Cash out if still spinning and reached auto-collect amount
                }

                yield return new WaitForSeconds(1); // Wait before next spin
            }
            else
            {
                Debug.LogError("Already spinning!");
                break;
            }
        }
    }

    // Method to set the RTP and update the house earnings and money out there
    public void SetRTP(float rtpInput)
    {
        this.rtp = rtpInput;
        UpdateRTP();
    }

    // Method to update the house earnings and money out there based on the RTP
    private void UpdateRTP()
    {
        _houseEarnings = (100 - rtp) / 100 * depositAmount;
        _moneyOutThere = rtp / 100 * depositAmount;

        Debug.Log($"House earnings: {_houseEarnings} and Money out there: {_moneyOutThere}");
    }

    public void SetDepositAmount(float amount)
    {
        depositAmount = depositAmount; // amount;
        UpdateRTP();
    }

    void ToggleDropdownVisibility()
    {
        // If dropdown is active (visible), make it inactive (invisible)
        // If it is inactive (invisible), make it active (visible)
        BetAmountDropdown.SetActive(!BetAmountDropdown.activeSelf);
    }


    void Update()
    {
        if (Input.GetKeyDown(juggleKey))
        {
            float distanceToBall = Vector3.Distance(transform.position, ball.transform.position);
            if (distanceToBall <= juggleRadius)
            {
                animator.SetTrigger("Juggle");
            }
        }
    }

    void OnBetAmountChanged(int index)
    {
        string optionText = betAmountDropdown.options[index].text;
        int betAmount;

        if (int.TryParse(optionText, out betAmount))
        {
            betAmountInputField.text = optionText;

            // Determine the increment/decrement step based on the selected bet amount
            if (betAmount == 10 || betAmount == 20 || betAmount == 50)
            {
                incrementStep = 10;
                decrementStep = 10;
            }
            else
            {
                incrementStep = 50;
                decrementStep = 50;
            }
        }
        else
        {
            Debug.LogError("Dropdown option is not a valid number: " + optionText);
        }
    }

    void IncrementBetAmount()
    {
        int currentBet;
        if (int.TryParse(betAmountInputField.text, out currentBet))
        {
            currentBet += incrementStep;
            betAmountInputField.text = currentBet.ToString();
        }
        else
        {
            Debug.LogError("Bet amount is not a valid number: " + betAmountInputField.text);
        }
    }

    void DecrementBetAmount()
    {
        int currentBet;
        if (int.TryParse(betAmountInputField.text, out currentBet))
        {
            currentBet -= decrementStep;
            if (currentBet < 0)
            {
                currentBet = 0;
            }
            betAmountInputField.text = currentBet.ToString();
        }
        else
        {
            Debug.LogError("Bet amount is not a valid number: " + betAmountInputField.text);
        }
    }



    void StartGame()
    {
        messageDisplay.color = originalTextColor;
        messageDisplay.transform.localScale = originalTextScale;

        // Read and validate input
        wallet = float.Parse(walletInput.text);
        wager = float.Parse(wagerInput.text);
        multiplier = 0.7f;

        if (wager > wallet)
        {
            messageDisplay.text = "Wager cannot be greater than wallet amount.";
            return;
        }

        // Start the game
        gameRunning = true;
        StartCoroutine("RunGame");

        // Validate RTP input
        if (!float.TryParse(rtpInput.text, out float rtp) || rtp < 0 || rtp > 100)
        {
            messageDisplay.text = "RTP must be a number between 0 and 100.";
            return;
        }
        // Clear the message display before starting a new game
        messageDisplay.text = "";

        // Switch to stop button
        stopButtonImage.gameObject.SetActive(true);  // make the Stop button appear
        stopButtonImage.gameObject.GetComponent<Button>().onClick.AddListener(() => StopGame());
        stopButtonImage.gameObject.GetComponent<Button>().interactable = true;

        // Make play button disappear
        playButtonImage.gameObject.SetActive(false);

        coinImage.gameObject.SetActive(false);  // hide the coin image when a new game starts

        // Show all the multiplier images when game starts
        foreach (Image image in multiplierImages)
        {
            image.gameObject.SetActive(true);
        }

        // Start the multiplier updating coroutine
        StartCoroutine(UpdateMultiplier());
    }

    bool isUserStopped = false;

    void HideMultiplierImages()
    {
        foreach (Image image in multiplierImages)
        {
            image.gameObject.SetActive(false);
        }
    }


    void StopGame(bool isWin = true)
    {

        if (!isWin)
        {
            // Switch back to play button
            playButtonImage.gameObject.SetActive(true);  // make the Play button appear
            playButtonImage.gameObject.GetComponent<Button>().onClick.AddListener(() => StartGame());
            playButtonImage.gameObject.GetComponent<Button>().interactable = true;

            // Make stop button disappear
            stopButtonImage.gameObject.SetActive(false);

            HideMultiplierImages(); // Add this line to hide the multiplier images

            coinImage.gameObject.SetActive(false); // hide the coin image

            return;
        }

        // Declare winnings here
        float winnings = isWin ? wager * multiplier : 0;

        if (gameRunning)
        {
            messageDisplay.text = isWin ? "You have won " + winnings + " !" : "You Lose, Try Again!";
            messageDisplay.color = isWin ? gold : originalTextColor;
            messageDisplay.transform.localScale = isWin ? new Vector3(1.5f, 1.5f, 1.5f) : originalTextScale;
        }

        // Only allow stopping the game if it's running
        if (!gameRunning) return;

        gameRunning = false;
        wallet += winnings;
        walletDisplay.text = wallet.ToString();

        // Add the winnings to the winning history
        if (isWin)
        {
            winningHistory.Add(winnings);

            coinImage.gameObject.SetActive(true);
        }

        // Indicate that the game was stopped by the user or due to AutoCollect
        isUserStopped = true;

        // Switch back to play button
        playButtonImage.gameObject.SetActive(true);  // make the Play button appear
        playButtonImage.gameObject.GetComponent<Button>().onClick.AddListener(() => StartGame());
        playButtonImage.gameObject.GetComponent<Button>().interactable = true;

        // Make stop button disappear
        stopButtonImage.gameObject.SetActive(false);

        // Hide all the multiplier images when game stops
        foreach (Image image in multiplierImages)
        {
            image.gameObject.SetActive(false);
        }

        // Stop the multiplier updating coroutine
        StopCoroutine(UpdateMultiplier());

        if (isSpinning)
        {
            isSpinning = false;
            float cashOutAmount = currentMultiplier * betAmount;
            UpdateBalance(cashOutAmount);
            UpdateGameStatus("Auto-collected at: " + currentMultiplier);
        }
    }


    IEnumerator PopMultiplier()
    {
        // Change the color of the text to turquoise
        messageDisplay.color = new Color(0.251f, 0.878f, 0.816f);

        // Scale the text up to 150%
        messageDisplay.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

        yield return null;
    }

    IEnumerator RunGame()
    {
        // This is where we'd run the game logic (like the animation and random stop time)
        while (gameRunning)
        {
            // Increase the multiplier over time
            multiplier += Random.Range(0.01f, 1.2f);
            UpdateMultiplierDisplay(multiplier);
            // Display the Increasing multiplier
            StartCoroutine(PopMultiplier());

            yield return new WaitForSeconds(1.5f);

            // Random end condition
            float crashTimer = 0f;
            while (crashTimer <= Random.Range(1f, 4f))
            {
                crashTimer += Time.deltaTime;
                yield return null;
            }

            // Check if the game is still running before proceeding
            if (gameRunning && !isUserStopped)
            {
                gameRunning = false;
                wallet -= wager; // Subtract the wager from the wallet because the player lost
               /*  float rtp = float.Parse(rtpInput.text) / 100;
               float returnAmount = wager * rtp;
                wallet += returnAmount; // Add the returned RTP value to the wallet */
                walletDisplay.text = wallet.ToString();
                messageDisplay.text = "You Lose, Try Again!";

                StopGame(false); // Call StopGame with isWin = false
            }

            isUserStopped = false;
        }
    }

    IEnumerator UpdateMultiplier()
    {
        while (gameRunning)
        {
            // Increase the multiplier
            multiplier += 0.01f;

            // Update the display
            UpdateMultiplierDisplay(multiplier);

            // Wait for a fixed amount of time before updating again
            yield return new WaitForSeconds(0.1f);
        }
    }


    void UpdateMultiplierDisplay(float multiplier)
    {
        string multiplierString = multiplier.ToString("F2"); // Now consider two decimal places

        // Hide all the digit images
        foreach (Image image in multiplierImages)
        {
            image.gameObject.SetActive(false);
        }

        // Show and update the necessary digit images
        int spriteIndex = 0;  // To iterate over the multiplierImages array

        for (int i = 0; i < multiplierString.Length && spriteIndex < multiplierImages.Length; i++)
        {
            if (char.IsDigit(multiplierString[i]))
            {
                int digit = int.Parse(multiplierString[i].ToString());
                if (digit < numberSprites.Length)
                {
                    multiplierImages[spriteIndex].sprite = numberSprites[digit];
                    multiplierImages[spriteIndex].gameObject.SetActive(true);
                    spriteIndex++;
                }
            }
            else if (multiplierString[i] == '.')  // Handle the decimal point
            {
                multiplierImages[spriteIndex].sprite = numberSprites[10];  // 10 is the index of the decimal sprite
                multiplierImages[spriteIndex].gameObject.SetActive(true);
                spriteIndex++;
            }
        }

        // Update the slider's value
        multiplierSlider.value = multiplier;
    }




    void ToggleWinningHistory()
    {
        isHistoryDisplayed = !isHistoryDisplayed;

        if (isHistoryDisplayed)
        {
            // Display the winning history
            string historyText = "Winning History:\n";
            foreach (float win in winningHistory)
            {
                historyText += win + "\n";
            }
            winningHistoryDisplay.text = historyText;
        }
        else
        {
            // Hide the winning history
            winningHistoryDisplay.text = "";
        }
    }

    void AutoPlayGame()
    {
        if (!float.TryParse(autoCollectInput.text, out float autoCollectMultiplier) || autoCollectMultiplier <= 1 ||
            !int.TryParse(autoPlayInput.text, out int autoPlaySpins) || autoPlaySpins <= 0)
        {
            messageDisplay.text = "Invalid AutoPlay or AutoCollect values.";
            return;
        }

        // Use local variables for wager and wallet in AutoPlayGame
        autoPlayWallet = wallet;
        autoPlayWager = wager;

        // Start the AutoPlayGame Coroutine
        StartCoroutine(RunAutoPlayGame(autoCollectMultiplier, autoPlaySpins));
    }

    IEnumerator RunAutoPlayGame(float autoCollectMultiplier, int autoPlaySpins)
    {
        for (int i = 0; i < autoPlaySpins; i++)
        {
            gameRunning = true;
            multiplier = 1f;

            // Make the AutoCollect value vary randomly in each round
            float currentAutoCollectMultiplier = Random.Range(1.2f, 2.0f);

            while (gameRunning)
            {
                // Increase the multiplier over time
                multiplier += baseMultiplierIncrement;
                UpdateMultiplierDisplay(multiplier);
                // Update the multiplier count in the message display
                messageDisplay.text = "Multiplier: " + multiplier.ToString("F2");

                // Stop the game automatically if the multiplier has reached the AutoCollect value
                if (multiplier >= currentAutoCollectMultiplier)
                {
                    StopGame();
                    yield return new WaitForSeconds(2);
                    break;
                }

                yield return null;
            }
        }
    }
}
