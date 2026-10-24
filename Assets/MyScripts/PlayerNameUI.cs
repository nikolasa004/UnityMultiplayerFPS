using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameUI : MonoBehaviour
{
    [SerializeField] private GameObject namePanel;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button confirmButton;

    private PlayerManager player;
    private Local controller;
    private bool submitted;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(SubmitName);

        if (nameInput != null)
        {
            nameInput.characterLimit = 20;
            nameInput.lineType = TMP_InputField.LineType.SingleLine;
            nameInput.onSubmit.AddListener(OnInputSubmitted);
        }
    }

    public void Initialize(PlayerManager localPlayer, Local localController)
    {
        player = localPlayer;
        controller = localController;

        if (namePanel == null || nameInput == null || confirmButton == null)
        {
            Debug.LogError("PlayerNameUI references are not set on the HUD prefab.");
            return;
        }

        submitted = false;
        namePanel.SetActive(true);
        controller.SetNameEntryOpen(true);
        nameInput.SetTextWithoutNotify("");
        nameInput.Select();
        nameInput.ActivateInputField();
    }

    private void OnInputSubmitted(string value)
    {
        SubmitName();
    }

    public void SubmitName()
    {
        if (submitted || player == null || controller == null || nameInput == null)
            return;

        string name = nameInput.text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            nameInput.ActivateInputField();
            return;
        }

        submitted = true;
        player.CmdSetPlayerName(name);
        namePanel.SetActive(false);
        controller.SetNameEntryOpen(false);
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(SubmitName);
        if (nameInput != null)
            nameInput.onSubmit.RemoveListener(OnInputSubmitted);
    }
}
