using TMPro;
using System;
using UnityEngine;

// "정말 ~할까요?" 확인 팝업. 예를 누르면 Show에 넘긴 동작을 실행하고 닫힌다.
public sealed class ConfirmPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text messageLabel;

    private Action onConfirm;

    public void Show(string koreanMessage, Action confirm)
    {
        messageLabel.text = LocalizationTable.T(koreanMessage);
        onConfirm = confirm;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Confirm()
    {
        Action action = onConfirm;
        Close();
        action?.Invoke();
    }

    public void Close()
    {
        onConfirm = null;
        gameObject.SetActive(false);
    }
}
