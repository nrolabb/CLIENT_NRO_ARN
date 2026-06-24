namespace Game1
{
	public class TabClanDistribute : IActionListener
	{
		private int x;
		private int y;
		private int w = 170;
		private int h = 170;
		private int cmtoX;
		private int cmx;
		private int cmvx;
		private int cmdx;
		private int selected;
		private int lastSelected;
		private const int ITEM_HEIGHT = 24;
		private const int LIST_TOP = 42;
		private Command left;
		private Command right;
		private Scroll scroll;
		private MyVector members;
		private int[] quantities;
		private Item item;
		private sbyte itemIndex;
		public bool isShow;

		public TabClanDistribute()
		{
			left = new Command("Phân phối", this, 1, null);
			right = new Command(mResources.CLOSE, this, 2, null);
		}

		public void show(Item item, sbyte itemIndex, MyVector members)
		{
			this.item = item;
			this.itemIndex = itemIndex;
			this.members = members;
			int memberCount = (members == null) ? 0 : members.size();
			quantities = new int[memberCount];
			selected = (memberCount > 0) ? 0 : -1;
			lastSelected = selected;
			x = GameCanvas.w / 2 - w / 2;
			y = GameCanvas.h / 2 - h / 2;
			if (GameCanvas.h < 240)
			{
				y -= 10;
			}
			cmx = x;
			cmtoX = 0;
			if (GameCanvas.isTouch)
			{
				left.x = x;
				left.y = y + h + 5;
				right.x = x + w - 68;
				right.y = y + h + 5;
			}
			scroll = new Scroll();
			scroll.setStyle(memberCount, ITEM_HEIGHT, x, y + LIST_TOP, w, h - LIST_TOP, styleUPDOWN: true, 1);
			isShow = true;
		}

		private int getTotalQuantity()
		{
			int total = 0;
			for (int i = 0; i < quantities.Length; i++)
			{
				total += quantities[i];
			}
			return total;
		}

		private void changeQuantity(int index, int amount)
		{
			if (index < 0 || index >= quantities.Length)
			{
				return;
			}
			if (amount < 0 && quantities[index] > 0)
			{
				quantities[index]--;
			}
			else if (amount > 0 && item != null && getTotalQuantity() < item.quantity)
			{
				quantities[index]++;
			}
		}

		public void hide()
		{
			cmtoX = x + w;
			SmallImage.clearHastable();
		}

		public void paint(mGraphics g)
		{
			g.translate(-cmx, 0);
			PopUp.paintPopUp(g, x, y - 17, w, h + 17, -1, isButton: true);
			mFont.tahoma_7b_dark.drawString(g, "PHÂN PHỐI", x + w / 2, y - 7, mFont.CENTER);
			if (item != null)
			{
				SmallImage.drawSmallImage(g, item.template.iconID, x + 17, y + 18, 0, 3);
				mFont.tahoma_7b_dark.drawString(g, item.template.name, x + 32, y + 7, mFont.LEFT);
				mFont.tahoma_7_blue.drawString(g, getTotalQuantity() + "/" + item.quantity, x + 32, y + 20, mFont.LEFT);
			}
			g.setClip(x, y + LIST_TOP, w, h - LIST_TOP - 8);
			if (scroll != null)
			{
				g.translate(0, -scroll.cmy);
			}
			int memberCount = (members == null) ? 0 : members.size();
			for (int i = 0; i < memberCount; i++)
			{
				int rowY = y + LIST_TOP + i * ITEM_HEIGHT;
				if (rowY + ITEM_HEIGHT - ((scroll == null) ? 0 : scroll.cmy) < y + LIST_TOP || rowY - ((scroll == null) ? 0 : scroll.cmy) > y + h)
				{
					continue;
				}
				if (i == lastSelected)
				{
					g.setColor(15196114);
					g.fillRect(x + 4, rowY, w - 8, ITEM_HEIGHT - 1);
				}
				Member member = (Member)members.elementAt(i);
				mFont.tahoma_7_grey.drawString(g, member.name, x + 8, rowY + 6, mFont.LEFT);
				g.setColor(16711680);
				g.fillRect(x + w - 62, rowY + 4, 16, 16);
				mFont.tahoma_7_white.drawString(g, "-", x + w - 54, rowY + 5, mFont.CENTER);
				mFont.tahoma_7b_yellow.drawString(g, string.Empty + quantities[i], x + w - 35, rowY + 6, mFont.CENTER);
				g.setColor(65280);
				g.fillRect(x + w - 22, rowY + 4, 16, 16);
				mFont.tahoma_7_white.drawString(g, "+", x + w - 14, rowY + 5, mFont.CENTER);
			}
			g.translate(0, -g.getTranslateY());
			g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
			GameCanvas.paintz.paintCmdBar(g, left, null, right);
		}

		public void update()
		{
			if (scroll != null)
			{
				scroll.updatecm();
			}
			if (cmx != cmtoX)
			{
				cmvx = cmtoX - cmx << 2;
				cmdx += cmvx;
				cmx += cmdx >> 3;
				cmdx &= 15;
			}
			if (Math.abs(cmtoX - cmx) < 10)
			{
				cmx = cmtoX;
			}
			if (cmx >= x + w - 10 && cmtoX >= x + w - 10)
			{
				isShow = false;
			}
		}

		public void updateKey()
		{
			if (left != null && (GameCanvas.keyPressed[12] || mScreen.getCmdPointerLast(left)))
			{
				left.performAction();
			}
			if (right != null && (GameCanvas.keyPressed[13] || mScreen.getCmdPointerLast(right)))
			{
				right.performAction();
			}
			if (scroll != null && GameCanvas.isTouch)
			{
				ScrollResult result = scroll.updateKey();
				selected = scroll.selectedItem;
				if (result.isFinish && selected >= 0 && selected < quantities.Length)
				{
					lastSelected = selected;
					if (GameCanvas.px >= x + w - 68 && GameCanvas.px <= x + w - 42)
					{
						changeQuantity(selected, -1);
					}
					else if (GameCanvas.px >= x + w - 28 && GameCanvas.px <= x + w - 2)
					{
						changeQuantity(selected, 1);
					}
				}
			}
			if (GameCanvas.keyPressed[(!Main.isPC) ? 2 : 21] && quantities.Length > 0)
			{
				selected--;
				if (selected < 0)
				{
					selected = quantities.Length - 1;
				}
				lastSelected = selected;
				scroll.moveTo(selected * ITEM_HEIGHT);
			}
			if (GameCanvas.keyPressed[(!Main.isPC) ? 8 : 22] && quantities.Length > 0)
			{
				selected++;
				if (selected >= quantities.Length)
				{
					selected = 0;
				}
				lastSelected = selected;
				scroll.moveTo(selected * ITEM_HEIGHT);
			}
			if (GameCanvas.keyPressed[(!Main.isPC) ? 4 : 23])
			{
				changeQuantity(lastSelected, -1);
			}
			if (GameCanvas.keyPressed[(!Main.isPC) ? 6 : 24])
			{
				changeQuantity(lastSelected, 1);
			}
			GameCanvas.clearKeyHold();
			GameCanvas.clearKeyPressed();
		}

		public void perform(int idAction, object p)
		{
			if (idAction == 2)
			{
				hide();
				return;
			}
			if (idAction != 1)
			{
				return;
			}
			MyVector distributions = new MyVector();
			for (int i = 0; i < quantities.Length; i++)
			{
				if (quantities[i] > 0)
				{
					Member member = (Member)members.elementAt(i);
					distributions.addElement(new Service.DistributeTarget(member.ID, quantities[i]));
				}
			}
			if (distributions.size() == 0)
			{
				GameCanvas.startOKDlg("Vui lòng chọn ít nhất 1 thành viên để phân phối!");
				return;
			}
			Service.gI().distributeClanBoxItem(itemIndex, distributions);
			hide();
			if (GameCanvas.panel2 != null && GameCanvas.panel2.isClanBox)
			{
				GameCanvas.panel2.requestClanBoxRefresh();
			}
		}
	}
}
