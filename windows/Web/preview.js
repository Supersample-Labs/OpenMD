let queue = Promise.resolve();
window.openmdRender = (html, dark) => {
  window.openmdBusy = true;
  window.openmdErrors = 0;
  queue = queue.catch(() => {}).then(async () => {
    document.documentElement.dataset.theme = dark ? 'dark' : 'light';
    const content = document.getElementById('content');
    content.innerHTML = html;
    mermaid.initialize({startOnLoad:false, securityLevel:'strict', theme:dark?'dark':'default', suppressErrorRendering:true, flowchart:{htmlLabels:false}});
    let index = 0;
    for (const code of content.querySelectorAll('pre.mermaid, pre > code.language-mermaid')) {
      const box = document.createElement('div');
      box.className = 'diagram';
      (code.tagName === 'PRE' ? code : code.parentElement).replaceWith(box);
      try {
        const {svg} = await mermaid.render('diagram' + (++window.openmdId), code.textContent);
        box.innerHTML = svg;
      } catch (error) {
        window.openmdErrors++;
        box.className = 'diagram-error';
        box.textContent = 'Mermaid diagram ' + (++index) + ': ' + error.message;
      }
    }
    await document.fonts.ready;
  }).finally(() => { window.openmdBusy = false; });
};
window.openmdId = 0;

