﻿using CommonControls.PackFileBrowser;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.Common
{
    public interface IDropTarget
    {
        bool AllowDrop(TreeNode node, TreeNode targeNode = null);
        bool Drop(TreeNode node, TreeNode targeNode = null);
    }
}
