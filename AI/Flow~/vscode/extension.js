// vscode://local.dungeon-flow/open?file=Map/Chest.cs&from=19&to=26 -> opens it, a box around lines 19-26
const v = require('vscode');
const box = w => v.window.createTextEditorDecorationType({isWholeLine:true, borderStyle:'solid', borderWidth:w,
  borderColor:new v.ThemeColor('focusBorder'), backgroundColor:new v.ThemeColor('editor.rangeHighlightBackground')});
const one = box('2px'), top = box('2px 2px 0 2px'), mid = box('0 2px'), bot = box('0 2px 2px 2px');
let flow; // AI/Flow~/flow.json: file paths start where AI/ is

exports.activate = ctx => ctx.subscriptions.push(v.window.registerUriHandler({async handleUri(uri){
  const q = new URLSearchParams(uri.query), file = q.get('file'), a = +q.get('from')-1, b = +q.get('to')-1;
  flow ??= (await v.workspace.findFiles('**/AI/Flow~/flow.json', null, 1))[0];
  const doc = flow ? flow.with({path: flow.path.replace(/AI\/Flow~\/flow\.json$/, file)}) : (await v.workspace.findFiles('**/'+file, null, 1))[0];
  if(!doc)return v.window.showWarningMessage('Dungeon Flow: '+file+' is not in this workspace');
  const ed = await v.window.showTextDocument(doc);
  const at = (from, to) => Array.from({length:Math.max(to-from+1,0)}, (_,i) => new v.Range(from+i,0,from+i,0));
  for(const e of v.window.visibleTextEditors)for(const d of [one,top,mid,bot])e.setDecorations(d, []);
  ed.setDecorations(one, a===b ? at(a,a) : []);
  ed.setDecorations(top, a<b ? at(a,a) : []);
  ed.setDecorations(mid, at(a+1,b-1));
  ed.setDecorations(bot, a<b ? at(b,b) : []);
  ed.selection = new v.Selection(a,0,a,0);
  ed.revealRange(new v.Range(a,0,b,0), v.TextEditorRevealType.InCenterIfOutsideViewport);
}}));
