using System.Linq;
using UnityEngine.SceneManagement;

namespace Game1
{
    
    public class TabController
    {
        private static TabController _Instance;
        public static TabController Instance => _Instance ?? (_Instance = new TabController());
    
        public TabController()
        {
            initCommand();
        }
    
        private static bool _selectTab;
    
        public static bool selectTab
        {
            get => _selectTab;
            set => _selectTab = value;
        }
    
        private static bool _isShow = true;
    
        public static bool isShow
        {
            get => _isShow;
            set => _isShow = value;
        }
    
        private static sbyte tabIndex = 0;
    
        private static TabCommand firstCommand = new TabCommand("Tab", () => showTabSelect());
    
        private static TabCommand[] TransferTab = Enumerable.Range(0, 3)
              .Select(i => new TabCommand((i + 1).ToString(), () => TransferTabIndex((sbyte)(i - 1))))
              .ToArray();
    
        private static string[] SceneNames = new string[]
        {
                "Nro1",
                "NRO2",
                "NRO3",
        };
        private static TabE[] tabTypes = new TabE[]
        {
                TabE.Tab1,
                TabE.Tab2
        };
        private static void initCommand()
        {
            firstCommand.x = GameCanvas.w - 80;
            firstCommand.y = 3;
            for (int i = 0; i < TransferTab.Length; i++)
            {
                TransferTab[i].x = GameCanvas.w - 80;
                TransferTab[i].y = 3 + (i + 1) * 37;
            }
        }
        public void paint(mGraphics g)
        {
            if (!isShow) return;
            if (GameScr.gI().isNotPaintTouchControl()) return;
            firstCommand.paint(g);
            paintTab(g);
            int currentTabIndex = -1;
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            for (int i = 0; i < SceneNames.Length; i++)
            {
                if (SceneNames[i] == currentScene)
                {
                    currentTabIndex = i;
                    break;
                }
            }
            mFont.tahoma_7b_red.drawString(g, "Tab " + (currentTabIndex + 1), firstCommand.x + 16, firstCommand.y + 28, 3);
    
        }
        private void paintTab(mGraphics g)
        {
            if (!selectTab) return;
            foreach (var cmd in TransferTab)
            {
                cmd.paint(g);
            }
        }
        private static void TransferTabIndex(sbyte index)
        {
            tabIndex = (sbyte)(index + 1);
    
            SceneManager.LoadScene(SceneNames[index + 1]);
            TabMn.tab = tabTypes[index + 1];
            _selectTab = false;
        }
        private static void showTabSelect()
        {
            _selectTab = !_selectTab;
        }
        public bool isPointerHoldInTab()
        {
            if (!isShow)
                return false;
            if (GameScr.gI().isNotPaintTouchControl())
                return false;
            if (firstCommand.isPointerInside())
            {
                firstCommand.Invoke();
                return true;
            }
            if (selectTab)
            {
                foreach (var cmd in TransferTab)
                {
                    if (cmd.isPointerInside())
                    {
                        cmd.Invoke();
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
