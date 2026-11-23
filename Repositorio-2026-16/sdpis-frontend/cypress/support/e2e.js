


Cypress.on('uncaught:exception', (err, runnable) => {
    if (err.message.includes("Cannot read properties of null (reading 'document')")) {
        return false;
    }
    return true;
});
