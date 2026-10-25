# VELTRIX Web (Angular 20)

The Angular client of VELTRIX. Setup, architecture and test instructions are in the [main README](../README.md).

```bash
npm install
npm start                                            # http://localhost:4200 (API must run on :5295)
npx ng test --watch=false --browsers=ChromeHeadless  # Jasmine/Karma
npx ng build                                         # production build → dist/
```
