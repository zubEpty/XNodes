using System;
using UnityEngine;

namespace Dispatch.Gameplay
{
[DisallowMultipleComponent]
public class DispatchBatteryStamina : MonoBehaviour
{
    public static DispatchBatteryStamina Instance { get; private set; }

    [Header("Charges")]
    [SerializeField, Min(0)] private int maxCharges = 3;
    [SerializeField, Min(0)] private int startingCharges = 3;
    [SerializeField, Min(0)] private int reservedGoalCharges;

    [Header("UI")]
    [SerializeField] private Transform powerContainer;
    [SerializeField] private GameObject[] powerObjects;

    public event Action<int, int> OnChargesChanged;

    public int CurrentCharges { get; private set; }
    public int MaxCharges => maxCharges;
    public int StartingCharges
    {
        get => startingCharges;
        set
        {
            startingCharges = Mathf.Max(0, value);
            maxCharges = Mathf.Max(maxCharges, startingCharges);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple DispatchBatteryStamina instances found. Keeping the newest instance.");
        }

        Instance = this;
        AutoBindPowerObjects();
        ResetToStartingCharges();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void InitializeFromLevelData(DispatchNodeLevelData levelData)
    {
        if (levelData != null)
            StartingCharges = levelData.StartingBatteryCharges;

        ResetToStartingCharges();
    }

    public void ResetToStartingCharges()
    {
        SetCharges(startingCharges);
    }

    public void SetCharges(int charges)
    {
        CurrentCharges = Mathf.Clamp(charges, 0, maxCharges);
        RefreshPowerObjects();
        OnChargesChanged?.Invoke(CurrentCharges, maxCharges);
    }

    public bool CanSpendForAction(int amount = 1, bool isGoalAction = false)
    {
        if (amount <= 0)
            return true;

        if (isGoalAction)
            return CurrentCharges >= amount;

        return CurrentCharges - amount >= reservedGoalCharges;
    }

    public bool TrySpendForAction(int amount = 1, bool isGoalAction = false)
    {
        if (!CanSpendForAction(amount, isGoalAction))
            return false;

        SetCharges(CurrentCharges - Mathf.Max(0, amount));
        return true;
    }

    private void AutoBindPowerObjects()
    {
        if (powerContainer == null)
        {
            Transform container = transform.Find("PowerContainer");
            if (container != null)
                powerContainer = container;
        }

        if ((powerObjects != null && powerObjects.Length > 0) || powerContainer == null)
            return;

        int childCount = powerContainer.childCount;
        powerObjects = new GameObject[childCount];

        for (int i = 0; i < childCount; i++)
            powerObjects[i] = powerContainer.GetChild(i).gameObject;

        maxCharges = Mathf.Max(maxCharges, childCount);
    }

    private void RefreshPowerObjects()
    {
        AutoBindPowerObjects();

        if (powerObjects == null)
            return;

        for (int i = 0; i < powerObjects.Length; i++)
        {
            if (powerObjects[i] != null)
                powerObjects[i].SetActive(i < CurrentCharges);
        }
    }
}
}
