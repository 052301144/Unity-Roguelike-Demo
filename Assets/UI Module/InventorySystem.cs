using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InventorySystem : MonoBehaviour
{
    [Header("����UI")]
    public GameObject inventoryPanel; // �������
    public Button closeButton; // �رհ�ť����ѡ��

    [Header("��������")]
    public KeyCode toggleKey = KeyCode.B; // ���ؿ�ݼ�
    public bool pauseGameWhenOpen = true; // ��ʱ�Ƿ���ͣ��Ϸ

    private bool isInventoryOpen = false;
    private CanvasGroup canvasGroup; // ���ڵ��뵭��Ч������ѡ��

    void Start()
    {
        InitializeInventory();
    }

    void InitializeInventory()
    {
        // ȷ��������ʼʱ�����ص�
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);

            // ���CanvasGroup���ڿ��ܵĶ���Ч��
            canvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = inventoryPanel.AddComponent<CanvasGroup>();
            }
        }

        // �󶨹رհ�ť�¼�
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInventory);
        }
        else
        {
            // �Զ����ҹرհ�ť
            FindCloseButton();
        }

        Debug.Log("����ϵͳ��ʼ����ɣ��� " + toggleKey + " ����/�ر�");
    }

    void Update()
    {
        // ����ݼ�
        if (Input.GetKeyDown(toggleKey))
        {
            // �����ͣ�˵��Ƿ��
            SimplePauseMenu pauseMenu = FindObjectOfType<SimplePauseMenu>();
            SimplePauseMenu pauseSetting = FindObjectOfType<SimplePauseMenu>();
            if (pauseMenu != null && pauseMenu.IsMenuOpen()|| pauseSetting != null && pauseSetting.IssettingsOpen())
            {
                Debug.Log("��ͣ�˵��Ѵ򿪣��޷���������");
                return; // �����ͣ�˵��򿪣���ִ�б�������
            }

            ToggleInventory();
        }

        // ESC��Ҳ���Թرձ�������ѡ��
        if (isInventoryOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInventory();
        }
    }

    void FindCloseButton()
    {
        if (inventoryPanel != null)
        {
            Transform closeBtnTransform = inventoryPanel.transform.Find("CloseButton");
            if (closeBtnTransform == null)
            {
                closeBtnTransform = inventoryPanel.transform.Find("ExitButton");
            }
            if (closeBtnTransform == null)
            {
                closeBtnTransform = inventoryPanel.transform.Find("BackButton");
            }

            if (closeBtnTransform != null)
            {
                closeButton = closeBtnTransform.GetComponent<Button>();
                if (closeButton != null)
                {
                    closeButton.onClick.AddListener(CloseInventory);
                    Debug.Log("�Զ��ҵ��رհ�ť: " + closeButton.name);
                }
            }
        }
    }

    // �л�������ʾ/����
    public void ToggleInventory()
    {
        if (isInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    // �򿪱���
    public void OpenInventory()
    {
        if (inventoryPanel != null && !isInventoryOpen)
        {
            inventoryPanel.SetActive(true);
            isInventoryOpen = true;

            // ��ͣ��Ϸ
            if (pauseGameWhenOpen)
            {
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }

            // ��ѡ�����Ŵ򿪶���
            StartCoroutine(PlayOpenAnimation());

            Debug.Log("�����Ѵ�");
        }
    }

    // �رձ���
    public void CloseInventory()
    {
        if (inventoryPanel != null && isInventoryOpen)
        {
            // ��ѡ�����Źرն���
            StartCoroutine(PlayCloseAnimation());

            Debug.Log("�����ѹر�");
        }
    }

    // ���Ŵ򿪶�������ѡ��
    IEnumerator PlayOpenAnimation()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            inventoryPanel.SetActive(true);

            float duration = 0.2f;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime; // ʹ��unscaledʱ�䣬��Ϊ��Ϸ������ͣ��
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }
    }

    // ���Źرն�������ѡ��
    IEnumerator PlayCloseAnimation()
    {
        if (canvasGroup != null)
        {
            float duration = 0.15f;
            float timer = 0f;
            float startAlpha = canvasGroup.alpha;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / duration);
                yield return null;
            }
        }

        inventoryPanel.SetActive(false);
        isInventoryOpen = false;

        // �ָ���Ϸ
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }

    // ǿ�ƹرձ���������Ҫʱ���ã�
    public void ForceCloseInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isInventoryOpen = false;

            // �ָ���Ϸ
            Time.timeScale = 1f;
            AudioListener.pause = false;

            Debug.Log("����ǿ�ƹر�");
        }
    }

    // ��鱳���Ƿ�򿪣��������ű���ѯ��
    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }

    // �ڱ༭���в��Եķ���
    [ContextMenu("���Դ򿪱���")]
    public void TestOpenInventory()
    {
        OpenInventory();
    }

    [ContextMenu("���Թرձ���")]
    public void TestCloseInventory()
    {
        CloseInventory();
    }
}