#!/bin/sh
cd "${TRAVIS_BUILD_DIR}/Tools/DocCollector" || exit
git clone "https://${GITHUB_TOKEN}@github.com/imazen/resizer-web.git"
git config user.name "Imazen Bot"
git config user.email "codebot@imazen.io"
git remote add pr "https://${GITHUB_TOKEN}@github.com/imazen-bot/resizer-web.git"
cd "${TRAVIS_BUILD_DIR}/Tools/DocCollector/resizer-web" || exit
git checkout -f master
git pull
cd "${TRAVIS_BUILD_DIR}/Tools/DocCollector" || exit
bundle -j4
bundle exec rake resizer
cd "${TRAVIS_BUILD_DIR}/Tools/DocCollector/resizer-web" || exit
git checkout -b "${TRAVIS_BRANCH}_docs_${TRAVIS_BUILD_NUMBER}"
git add .
git commit -m "DocCollector Update for imazen/resizer#${TRAVIS_PULL_REQUEST}"
git push imazen-bot/resizer-web "${TRAVIS_BRANCH}_docs_${TRAVIS_BUILD_NUMBER}"
# resizer-web requires ruby 2.4 and cpp travis uses 2.2.6
#gem install hub
#hub pull-request -m "DocCollector Update for ${TRAVIS_PULL_REQUEST}" -b "imazen/resizer-web:production"