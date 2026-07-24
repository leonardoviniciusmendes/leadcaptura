<template>
  <div class="shell">
    <header class="topbar">
      <RouterLink class="brand" to="/campanhas">LeadEngine</RouterLink>
      <nav>
        <RouterLink to="/campanhas">Campanhas</RouterLink>
        <RouterLink to="/campanhas/nova">Nova campanha</RouterLink>
      </nav>
    </header>
    <RouterView />
  </div>
</template>
