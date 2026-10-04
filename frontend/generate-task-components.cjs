const fs = require('fs');
const path = require('path');

const componentsDir = path.join(__dirname, 'src', 'components', 'Tasks');

if (!fs.existsSync(componentsDir)) {
  fs.mkdirSync(componentsDir, { recursive: true });
}

const files = [
  'TaskList.jsx',
  'TaskListHeader.jsx',
  'TaskFilters.jsx',
  'TaskCard.jsx',
  'TaskTable.jsx',
  'TaskKanbanBoard.jsx',
  'CreateTaskModal.jsx',
  'EditTaskModal.jsx',
  'TaskDetailModal.jsx',
  'TaskDetailTabs.jsx',
  'TaskOverview.jsx',
  'TaskComments.jsx',
  'CommentCard.jsx',
  'CommentForm.jsx',
  'TaskAttachments.jsx',
  'AttachmentCard.jsx',
  'AttachmentUpload.jsx',
  'TaskActivity.jsx',
  'ActivityItem.jsx',
  'StatusBadge.jsx',
  'PriorityBadge.jsx',
  'StatusWorkflow.jsx',
  'EmptyState.jsx',
  'index.js'
];

files.forEach(file => {
  const filePath = path.join(componentsDir, file);
  if (!fs.existsSync(filePath)) {
    const componentName = file.replace('.jsx', '');
    let content = `import React from 'react';\n\nexport const ${componentName} = () => {\n  return (\n    <div>${componentName}</div>\n  );\n};\n`;
    if (file === 'index.js') {
      content = files.filter(f => f !== 'index.js').map(f => `export * from './${f.replace('.jsx', '')}';`).join('\n') + '\n';
    }
    fs.writeFileSync(filePath, content);
  }
});

console.log('Task components generated successfully!');
