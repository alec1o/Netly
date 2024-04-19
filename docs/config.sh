### clone repository and change branch ###
BASEDIR=$(dirname $0)
MY_FOLDER="_build"

#clean
rm -f -r  ${BASEDIR}/${MY_FOLDER}/

# version list
# debug:
# declare -a versions=("v1.0.0" "v2.0.0" "dev")
# release:
declare -a versions=("v1.0.0" "v1.1.0" "v2.0.0" "v2.1.0" "v2.1.1" "v2.2.0" "v2.3.0" "v2.4.0" "v2.5.0" "v2.5.1" "v2.5.2" "v3.0.0" "v3.1.0" "dev")

for version in "${versions[@]}"
do
    echo "Config $version"...
    git clone ${BASEDIR}/../.git ${BASEDIR}/${MY_FOLDER}/$version
    git --git-dir=${BASEDIR}/${MY_FOLDER}/$version/.git --work-tree=${BASEDIR}/${MY_FOLDER}/$version/ checkout -f $version
done
