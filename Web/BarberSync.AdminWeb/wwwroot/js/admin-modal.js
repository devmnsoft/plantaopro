window.BarberSyncModal={prompt(title,fields,onSubmit){const data={};fields.forEach(f=>data[f]=window.prompt(`${title} - ${f}`)||'Demo');onSubmit(data);}};
