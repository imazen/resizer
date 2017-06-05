#!/bin/sh

# this should be set to the travis build dir
if [ "${TRAVIS_BUILD_DIR}" = "" ]; then
  exit 1
fi

# this should come from encrypted travis stuff
if [ "${GITHUB_TOKEN}" = "" ]; then
  exit 2
fi


# this should be set to the branch name
if [ "${TRAVIS_BRANCH}" = "" ]; then
  exit 3
fi

# TRAVIS_PULL_REQUEST should be unset
if [ ! "${TRAVIS_PULL_REQUEST}" = "" ]; then
  exit 4
fi

cd "${TRAVIS_BUILD_DIR}/Tools/DocCollector" || exit
git clone "https://${GITHUB_TOKEN}@github.com/imazen/resizer-web.git"
git config --global user.name "Imazen Bot"
git config --global user.email "codebot@imazen.io"
cd "${TRAVIS_BUILD_DIR}/Tools/DocCollector/resizer-web" || exit
git remote add pr "https://${GITHUB_TOKEN}@github.com/imazen-bot/resizer-web.git"
git checkout -f production
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